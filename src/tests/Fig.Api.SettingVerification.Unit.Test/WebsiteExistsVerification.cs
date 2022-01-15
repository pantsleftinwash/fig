using System.Net;
using System.Net.Http;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.SettingVerification.Unit.Test;

public class WebsiteExistsVerification : ISettingVerification
{
    private const string Websiteaddress = "WebsiteAddress";

    public VerificationResultDataContract PerformVerification(System.Collections.Generic.Dictionary<string, System.Object> settingValues)
    {
        var result = new VerificationResultDataContract();
        using HttpClient client = new HttpClient();

        if (!settingValues.ContainsKey(Websiteaddress))
        {
            result.Message = "No Setting Found";
            return result;
        }

        var requestResult = client.GetAsync((string) settingValues[Websiteaddress]).Result;

        if (requestResult.StatusCode == HttpStatusCode.OK)
        {
            result.Message = "Succeeded";
            result.Success = true;
            return result;
        }

        result.Message = $"Failed with response: {requestResult.StatusCode}";
        return result;
    }
}