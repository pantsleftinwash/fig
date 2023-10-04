﻿using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Common.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Newtonsoft.Json;

namespace Fig.Api.DataImport;

public class ConfigFileImporter : BackgroundService
{
    private const string JsonFilter = "*.json";
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IFileImporter _fileImporter;
    private readonly ILogger<ConfigFileImporter> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ConfigFileImporter(ILogger<ConfigFileImporter> logger,
        IFileImporter fileImporter,
        IServiceScopeFactory serviceScopeFactory,
        IConfigurationRepository configurationRepository)
    {
        _logger = logger;
        _fileImporter = fileImporter;
        _serviceScopeFactory = serviceScopeFactory;
        _configurationRepository = configurationRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var path = GetImportFolderPath();
        await _fileImporter.Initialize(path, JsonFilter, ImportFile, CanImport);
    }

    private string GetImportFolderPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var path = Path.Combine(appData, "Fig", "ConfigImport");

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        return path;
    }

    private bool CanImport()
    {
        var configuration = _configurationRepository.GetConfiguration();
        return configuration.AllowFileImports;
    }

    private async Task ImportFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            _logger.LogInformation("Importing export file at path: {Path}", path);
            var text = await File.ReadAllTextAsync(path);

            if (text.TryParseJson(TypeNameHandling.Objects, out FigDataExportDataContract? fullImportData) && fullImportData?.ImportType != ImportType.UpdateValues)
            {
                Import(fullImportData, path);
            }
            else if (text.TryParseJson(TypeNameHandling.Objects, out FigValueOnlyDataExportDataContract? valueOnlyImportData))
            {
                ImportValueOnly(valueOnlyImportData, path);
            }
            else
            {
                throw new InvalidDataException("JSON file could not be deserialized");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid file for fig import: {Path}", path);
        }
        finally
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to delete file at path {Path}", path);
            }
        }
    }

    private void Import(FigDataExportDataContract? importData, string path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var importExportService = scope.ServiceProvider.GetService<IImportExportService>();
        if (importExportService is null)
            throw new InvalidOperationException("Unable to find ImportExport service");

        SetImportingUser(importExportService);
        var result = importExportService.Import(importData, ImportMode.FileLoad);
        _logger.LogInformation("Import of full settings file {Path} completed successfully. {Result}", path, result);
    }

    private void SetImportingUser(IImportExportService importExportService)
    {
        // The authenticated user is required for the client filtering.
        importExportService.SetAuthenticatedUser(new UserDataContract(Guid.NewGuid(),
            "SYSTEM",
            "File",
            "Import",
            Role.Administrator,
            ".*"));
    }

    private void ImportValueOnly(FigValueOnlyDataExportDataContract? importData, string path)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var importExportService = scope.ServiceProvider.GetService<IImportExportService>();
        if (importExportService is null)
            throw new InvalidOperationException("Unable to find ImportExport service");
        
        SetImportingUser(importExportService);
        var result = importExportService.ValueOnlyImport(importData, ImportMode.FileLoad);
        _logger.LogInformation("Import of value only file {Path} completed successfully. {Result}", path, result);
    }
}