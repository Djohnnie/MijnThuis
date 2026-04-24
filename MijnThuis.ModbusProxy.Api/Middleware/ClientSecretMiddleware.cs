namespace MijnThuis.ModbusProxy.Api.Middleware;

public class ClientSecretMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _clientSecret;

    public ClientSecretMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _clientSecret = configuration.GetValue<string>("CLIENT_SECRET") ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<ClientSecretMiddleware> logger)
    {
        var clientSecret = context.Request.Headers["X-Client-Secret"].FirstOrDefault();

        if (string.IsNullOrEmpty(_clientSecret) || clientSecret != _clientSecret)
        {
            logger.LogWarning("Unauthorized request from {RemoteIp} to {Path}", context.Connection.RemoteIpAddress, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
