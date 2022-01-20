using Fig.Api.Services;

namespace Fig.Api.Middleware;

public class CallerDetailsMiddleware
{
    private readonly RequestDelegate _next;

    public CallerDetailsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
        IEventLogFactory eventLogFactory,
        ISettingsService settingsService)
    {
        var ipAddress = context.Request.Headers["Fig_IpAddress"].FirstOrDefault();
        var hostname = context.Request.Headers["Fig_Hostname"].FirstOrDefault();
        eventLogFactory.SetRequesterDetails(ipAddress, hostname);
        settingsService.SetRequesterDetails(ipAddress, hostname);

        await _next(context);
    }
}