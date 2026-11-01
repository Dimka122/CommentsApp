using Comments.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        if (ex is not ValidationException ve) return false;
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(
            new ValidationProblemDetails(ve.Errors.ToDictionary(k => k.Key, k => k.Value)) { Status = 400, Title = "Validation failed" }, ct);
        return true;
    }
}
