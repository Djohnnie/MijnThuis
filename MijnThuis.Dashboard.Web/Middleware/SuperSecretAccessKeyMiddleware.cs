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

    public async Task InvokeAsync(HttpContext context, ExtraPageArguments extraPageArguments)
    {
        var accessKey = context.Request.Query["accessKey"];
        var isTesla = context.Request.Query["isTesla"];

        if (context.Request.Path == "/")
        {
            extraPageArguments.IsTesla = isTesla.Count > 0;
        }

        if ((context.Request.Path == "/" || context.Request.Path == "/solar") && accessKey != _superSecretAccessKey)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this resource.");
        }

        await _next(context);
    }
}