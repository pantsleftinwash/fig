using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.Services;

public interface ISettingsService : IAuthenticatedService
{
    IEnumerable<SettingsClientDefinitionDataContract> GetAllClients();

    IEnumerable<SettingDataContract> GetSettings(string clientName, string clientSecret, string? instance);

    Task RegisterSettings(string clientSecret, SettingsClientDefinitionDataContract clientDefinition);

    void DeleteClient(string clientName, string? instance);

    Task UpdateSettingValues(string clientName, string? instance, SettingValueUpdatesDataContract updatedSettings, bool clientOverride = false);

    Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName, string? instance);

    IEnumerable<SettingValueDataContract> GetSettingHistory(string clientName, string settingName, string? instance);

    IEnumerable<VerificationResultDataContract> GetVerificationHistory(string clientName, string verificationName,
        string? instance);

    ClientSecretChangeResponseDataContract ChangeSecret(string clientName, ClientSecretChangeRequestDataContract changeRequest);
}