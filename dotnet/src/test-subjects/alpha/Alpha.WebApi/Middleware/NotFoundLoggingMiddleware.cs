namespace Alpha.WebApi.Middleware;

public class NotFoundLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NotFoundLoggingMiddleware> _logger;

    public NotFoundLoggingMiddleware(RequestDelegate next, ILogger<NotFoundLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Process the request through the pipeline
        await _next(context);

        // Check if the response is a 404
        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            // Log the 404 error with request details
            _logger.LogError("404 Not Found: {Method} {Path} from {RemoteIp}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);
        }
    }
}