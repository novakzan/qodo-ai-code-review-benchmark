using System.Diagnostics;

namespace ECommerce.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var requestInfo = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] " +
                          $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString} " +
                          $"from {context.Connection.RemoteIpAddress} " +
                          $"User-Agent: {context.Request.Headers["User-Agent"]} " +
                          $"Content-Type: {context.Request.ContentType ?? "N/A"} " +
                          $"Content-Length: {context.Request.ContentLength?.ToString() ?? "0"}";

        _logger.LogInformation("Request started: {RequestInfo}", requestInfo);

        await _next(context);

        stopwatch.Stop();

        var responseInfo = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] " +
                           $"{context.Request.Method} {context.Request.Path} " +
                           $"responded {context.Response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms";

        _logger.LogInformation("Request completed: {ResponseInfo}", responseInfo);
    }
}
