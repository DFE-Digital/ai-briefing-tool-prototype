using BriefingTool.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BriefingTool.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private const string ApiKeyHeader = "x-api-key";
    private const string ApiKeyMissingResponse = "API key was not provided";
    private const string UnauthorisedResponse = "Unauthorised Client";
    private readonly RequestDelegate _next;
    private readonly ICollection<string> _apiKeys;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IOptions<AuthenticationConfig> options)
    {
        ArgumentNullException.ThrowIfNull(options.Value.ApiKeys);
        (_next, _apiKeys) = (next, options.Value.ApiKeys);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bypass API Key requirement for health check route
        if (context.Request.Path == "/healthcheck")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var requestApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(ApiKeyMissingResponse);
            return;
        }

        if (!_apiKeys.Contains(requestApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(UnauthorisedResponse);
            return;
        }

        await _next(context);
    }

   
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeader = "x-api-key";
    private const string ApiKeyMissingResponse = "API key was not provided";
    private const string UnauthorisedResponse = "Unauthorised Client";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var options = context.HttpContext.RequestServices.GetService(typeof(IOptions<AuthenticationConfig>)) as IOptions<AuthenticationConfig>;
        ArgumentNullException.ThrowIfNull(options?.Value.ApiKeys);
        var apiKeys = options.Value.ApiKeys;

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var requestApiKey))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Result = new ContentResult()
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Content = ApiKeyMissingResponse
            };
            return;
        }

        if (!apiKeys.Contains(requestApiKey!))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

            context.Result = new ContentResult()
            {
                StatusCode = 403,
                Content = UnauthorisedResponse
            };
            return;
        }

        await next();
    }
}