namespace MijnThuis.Dashboard.Web.Middleware;

public class SuperSecretAccessKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _superSecretAccessKey;

    public SuperSecretAccessKeyMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _superSecretAccessKey = configuration.GetValue<string>("SUPER_SECRET_ACCESS_KEY");
    }

    public async Task InvokeAsync(HttpContext context, ExtraPageArguments extraPageArguments, ILogger<SuperSecretAccessKeyMiddleware> logger)
    {
        var accessKey = context.Request.Query["accessKey"];
        var isTesla = context.Request.Query["isTesla"];
        var isAllowed = true;

        if (context.Request.Path == "/")
        {
            extraPageArguments.IsTesla = isTesla.Count > 0;
        }

        if ((context.Request.Path == "/" || context.Request.Path == "/power" || context.Request.Path == "/solar" || context.Request.Path == "/car" || context.Request.Path == "/heating") && accessKey != _superSecretAccessKey)
        {
            isAllowed = false;
        }

        if (isAllowed)
        {
            await _next(context);
        }
        else
        {
            logger.LogWarning("Access DENIED for path {Path} without valid access key!", context.Request.Path);
            await context.Response.WriteAsync("<html><head><title>Mijn Thuis</title></head><body><h1>Mijn Thuis</h1></body></html>");
        }
    }
}