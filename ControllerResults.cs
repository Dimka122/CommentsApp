using Microsoft.AspNetCore.Mvc;

namespace Comments.Api;

public static class ControllerResults
{
    /// <summary>HTTP 400 с ошибками по полям (формат ValidationProblemDetails, его разбирает фронтенд).</summary>
    public static IActionResult Invalid(this ControllerBase c, IReadOnlyDictionary<string, string[]> errors)
        => c.ValidationProblem(new ValidationProblemDetails(errors.ToDictionary(k => k.Key, k => k.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        });
}
