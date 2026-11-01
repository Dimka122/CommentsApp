using Comments.Api.Contracts;
using Comments.Application.Common;
using Comments.Application.Comments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Comments.Api.Controllers;

[ApiController, Route("api/comments")]
public class CommentsController(CommentService comments) : ControllerBase
{
    /// <summary>Корневые комментарии с деревом ответов. sortBy: CreatedAt | UserName | Email; по умолчанию LIFO.</summary>
    [HttpGet, EnableRateLimiting("read")]
    public Task<PagedResult<CommentDto>> List(
        [FromQuery] CommentSortBy sortBy = CommentSortBy.CreatedAt, [FromQuery] bool desc = true,
        [FromQuery] int page = 1, CancellationToken ct = default)
        => comments.GetPageAsync(sortBy, desc, page, ct);

    [HttpPost, EnableRateLimiting("write")]
    [RequestSizeLimit(7_000_000), RequestFormLimits(MultipartBodyLengthLimit = 7_000_000)]
    [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
    public async Task<IActionResult> Create([FromForm] CreateCommentForm form, CancellationToken ct)
    {
        var file = form.File is { Length: > 0 } f ? new UploadedFile(f.FileName, f.Length, f.OpenReadStream) : null;
        var cmd = new CreateCommentCommand(form.UserName, form.Email, form.Homepage, form.Text,
                                           form.ParentId, form.CaptchaId, form.CaptchaAnswer, file);
        var client = new ClientInfo(
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString());

        var dto = await comments.CreateAsync(cmd, client, ct);
        return Created($"/api/comments/{dto.Id}", dto);
    }

    /// <summary>Предпросмотр: тот же санитайзер, что и при сохранении.</summary>
    [HttpPost("preview"), EnableRateLimiting("write")]
    public PreviewResponse Preview([FromBody] PreviewRequest req) => new(comments.Preview(req.Text));
}
