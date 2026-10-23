using CommentsApp.CommentsApp.Application.Dtos;
using CommentsApp.CommentsApp.Application.Exceptions;
using CommentsApp.CommentsApp.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(CommentService service) : ControllerBase
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 25;

    [HttpPost]
    [RequestSizeLimit(600 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] CommentCreateDto request,
        IFormFile? file,
        CancellationToken ct)
    {
        try
        {
            var dto = await service.CreateAsync(request, file, ct);
            return CreatedAtAction(nameof(GetPage), new { }, dto);
        }
        catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) }); }
        catch (InvalidCaptchaException) { return BadRequest(new { error = "Неверный код CAPTCHA" }); }
        catch (FileValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? sort = "createdAt",
        [FromQuery] string? dir = "desc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await service.GetRootPageAsync(page, pageSize, sort, dir, ct);
        return Ok(result);
    }

    [HttpPost("preview")]
    public IActionResult Preview([FromForm] string text)
    {
        var html = service.Preview(text);
        return Ok(new { html });
    }
}
