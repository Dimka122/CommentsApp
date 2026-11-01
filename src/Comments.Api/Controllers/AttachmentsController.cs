using Comments.Application.Comments;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController, Route("api/attachments")]
public class AttachmentsController(CommentService comments) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var file = await comments.OpenAttachmentAsync(id, ct);
        if (file is null) return NotFound();
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(file.Content, file.ContentType);
    }
}
