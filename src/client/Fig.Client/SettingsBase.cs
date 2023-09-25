﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using Fig.Client.Attributes;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.EnvironmentVariables;
using Fig.Client.Events;
using Fig.Client.ExtensionMethods;
using Fig.Common.NetStandard;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Utils;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Newtonsoft.Json;

namespace Fig.Client;

public abstract class SettingsBase
{
    private readonly IDescriptionProvider _descriptionProvider;
    private readonly IEnvironmentVariableReader _environmentVariableReader;
    private readonly ISettingDefinitionFactory _settingDefinitionFactory;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly List<string> _configurationErrors = new();

    protected SettingsBase()
        : this(new DescriptionProvider(new InternalResourceProvider(),
                new MarkdownExtractor()),
            new DataGridDefaultValueProvider(),
            new EnvironmentVariableReader())
    {
    }

    private SettingsBase(IDescriptionProvider descriptionProvider, IDataGridDefaultValueProvider dataGridDefaultValueProvider, IEnvironmentVariableReader environmentVariableReader)
        : this(new SettingDefinitionFactory(descriptionProvider, dataGridDefaultValueProvider), 
            new IpAddressResolver(),
            descriptionProvider,
            environmentVariableReader)
    {
        _descriptionProvider = descriptionProvider;
    }

    protected SettingsBase(ISettingDefinitionFactory settingDefinitionFactory,
        IIpAddressResolver ipAddressResolver,
        IDescriptionProvider descriptionProvider,
        IEnvironmentVariableReader environmentVariableReader)
    {
        _settingDefinitionFactory = settingDefinitionFactory;
        _ipAddressResolver = ipAddressResolver;
        _descriptionProvider = descriptionProvider;
        _environmentVariableReader = environmentVariableReader;
    }

    public abstract string ClientName { get; }
    
    public abstract string ClientDescription { get; }

    public bool SupportsRestart => RestartRequested != null;

    public bool HasConfigurationError { get; private set; } = false;

    public event EventHandler<ChangedSettingsEventArgs>? SettingsChanged;

    public event EventHandler? RestartRequested;

    public void Initialize(IEnumerable<SettingDataContract>? settings)
    {
        if (settings != null)
            SetPropertiesFromSettings(settings.ToList());
        else
            SetPropertiesFromDefaultValues();
    }

    public void Update(IEnumerable<SettingDataContract> settings, List<string>? changedSettingNames = null)
    {
        SetPropertiesFromSettings(settings.ToList());
        SettingsChanged?.Invoke(this, new ChangedSettingsEventArgs(changedSettingNames ?? new List<string>()));
    }

    public void RequestRestart()
    {
        RestartRequested?.Invoke(this, EventArgs.Empty);
    }

    public SettingsClientDefinitionDataContract CreateDataContract(bool liveReload)
    {
        var settings = GetSettingProperties()
            .Select(settingProperty => _settingDefinitionFactory.Create(settingProperty, liveReload, this))
            .ToList();

        var clientSettingOverrides = _environmentVariableReader.ReadSettingOverrides(ClientName, settings);
        
        return new SettingsClientDefinitionDataContract(ClientName,
            _descriptionProvider.GetDescription(ClientDescription),
            GetInstance(),
            settings,
            GetVerifications(),
            clientSettingOverrides);
    }

    public void SetConfigurationErrorStatus(bool configurationError, List<string>? configurationErrors = null)
    {
        HasConfigurationError = configurationError;
        
        if (configurationErrors != null)
            _configurationErrors.AddRange(configurationErrors);
    }

    internal List<string> GetConfigurationErrors()
    {
        var result = _configurationErrors.ToList();
        _configurationErrors.Clear();
        return result;
    }

    private string? GetInstance()
    {
        var value = Environment.GetEnvironmentVariable($"{ClientName.Replace(" ", "")}_INSTANCE");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private List<SettingVerificationDefinitionDataContract> GetVerifications()
    {
        var verificationAttributes = GetType()
            .GetCustomAttributes(typeof(VerificationAttribute), true)
            .Cast<VerificationAttribute>();

        return verificationAttributes.Select(attribute =>
            new SettingVerificationDefinitionDataContract(attribute.Name, attribute.Name,
                attribute.SettingNames.ToList())).ToList();
    }

    private IEnumerable<PropertyInfo> GetSettingProperties()
    {
        return GetType().GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(SettingAttribute)));
    }

    private void SetPropertiesFromDefaultValues()
    {
        foreach (var property in GetSettingProperties()) 
            SetDefaultValue(property);
    }

    private void SetDefaultValue(PropertyInfo property)
    {
        if (property.GetCustomAttributes()
                .FirstOrDefault(a => a.GetType() == typeof(SettingAttribute)) is SettingAttribute settingAttribute)
        {
            var defaultValue = settingAttribute.GetDefaultValue(this);
            if (defaultValue != null)
                property.SetValue(this, property.PropertyType == typeof(SecureString)
                    ? defaultValue.ToString().ToSecureString()
                    : ReplaceConstants(defaultValue));
        }
    }

    private object? ReplaceConstants(object? originalValue)
    {
        if (originalValue is string originalString)
        {
            return originalString
                .Replace(SettingConstants.MachineName, Environment.MachineName)
                .Replace(SettingConstants.User, Environment.UserName)
                .Replace(SettingConstants.Domain, Environment.UserDomainName)
                .Replace(SettingConstants.IpAddress, _ipAddressResolver.Resolve())
                .Replace(SettingConstants.ProcessorCount, $"{Environment.ProcessorCount}")
                .Replace(SettingConstants.OsVersion, Environment.OSVersion.VersionString);
        }

        return originalValue;
    }

    private void SetPropertiesFromSettings(List<SettingDataContract> settings)
    {
        foreach (var property in GetSettingProperties())
        {
            var definition = settings.FirstOrDefault(a => a.Name == property.Name);

            if (definition?.Value?.GetValue() != null)
            {
                if (property.PropertyType.IsEnum)
                    SetEnumValue(property, this, definition.Value.GetValue());
                else if (property.PropertyType.IsSecureString())
                    property.SetValue(this, definition.Value.GetValue()?.ToString()?.ToSecureString());
                else if (property.PropertyType.IsSupportedBaseType())
                    property.SetValue(this, ReplaceConstants(definition.Value.GetValue()));
                else if (property.PropertyType.IsSupportedDataGridType())
                    SetDataGridValue(property, definition.Value.GetValue() as List<Dictionary<string, object>>);
                else
                    SetJsonValue(property, definition.Value.GetValue());
            }
            else
            {
                SetDefaultValue(property);
            }
        }
    }

    private void SetEnumValue(PropertyInfo property, object target, object? value)
    {
        if (!string.IsNullOrWhiteSpace(value?.ToString()))
        {
            var enumValue = Enum.Parse(property.PropertyType, value!.ToString());
            property.SetValue(target, enumValue);
        }
    }

    private void SetDataGridValue(PropertyInfo property, List<Dictionary<string, object>>? dataGridRows)
    {
        if (dataGridRows is null)
            return;
        
        if (!ListUtilities.TryGetGenericListType(property.PropertyType, out var genericType))
            return;

        var list = (IList) Activator.CreateInstance(property.PropertyType);
        foreach (var dataGridRow in dataGridRows)
        {
            // If the row is a basic type, we don't need to create and populate it.
            // We just get the value and add it to the collection.
            if (genericType!.IsSupportedBaseType())
            {
                list.Add(ConvertToType(dataGridRow.Single().Value, genericType!));
                continue;
            }

            var listItem = Activator.CreateInstance(genericType!);

            foreach (var column in dataGridRow)
            {
                var prop = genericType!.GetProperty(column.Key);
                if (prop?.PropertyType == typeof(int) && column.Value is long longValue)
                    prop.SetValue(listItem, (int?) longValue);
                else if (prop?.PropertyType.IsEnum == true && column.Value is string strValue)
                    SetEnumValue(prop, listItem, strValue);
                else if (prop?.PropertyType == typeof(TimeSpan))
                    prop.SetValue(listItem, TimeSpan.Parse((string) column.Value));
                else
                    prop?.SetValue(listItem, ReplaceConstants(column.Value));
            }

            list.Add(listItem);
        }

        property.SetValue(this, list);
    }

    private object ConvertToType(object value, Type type)
    {
        if (value.GetType() == type)
            return value;

        return Convert.ChangeType(value, type);
    }

    private void SetJsonValue(PropertyInfo property, object? value)
    {
        if (value is not null)
        {
            var deserializedValue = JsonConvert.DeserializeObject(value.ToString(), property.PropertyType);
            property.SetValue(this, deserializedValue);
        }
        else
        {
            property.SetValue(this, null);
        }
    }
}