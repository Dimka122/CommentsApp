using Comments.Application.Abstractions;
using Comments.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Comments.Api.Controllers;

[ApiController, Route("api/search")]
public class SearchController(ICommentSearch search) : ControllerBase
{
    [HttpGet, EnableRateLimiting("read")]
    public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        q = q?.Trim();
        if (string.IsNullOrEmpty(q)) throw new ValidationException("q", "Search query is required.");
        if (q.Length > 100) throw new ValidationException("q", "Search query is too long (max 100 characters).");

        try { return Ok(await search.SearchAsync(q, Math.Max(1, page), 25, ct)); }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails { Title = "Search is temporarily unavailable." });
        }
    }
}
