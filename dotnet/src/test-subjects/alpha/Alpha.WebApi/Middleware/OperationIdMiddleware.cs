namespace Alpha.WebApi.Middleware;

public class OperationIdMiddleware
{
    private readonly RequestDelegate _next;

    public OperationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        string operationId = Guid.NewGuid().ToString("N");
        context.Items["OperationId"] = operationId;
        context.Response.Headers["X-Operation-Id"] = operationId;

        await _next(context);
    }
}
