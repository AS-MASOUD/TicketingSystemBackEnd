namespace Ticketing.Services;

public sealed class CallerIdentityExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (CallerIdentityException exception) when (!context.Response.HasStarted)
        {
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(new { message = exception.Message }, context.RequestAborted);
        }
    }
}
