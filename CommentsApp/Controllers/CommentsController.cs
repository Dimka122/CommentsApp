using CommentsApp.Application.DTOs;
using CommentsApp.Application.Interfaces;      // если тут лежат исключения — иначе уберите
using CommentsApp.Application.Services;
using CommentsApp.CommentsApp.Application.Exceptions;
using CommentsApp.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApp.Api.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(CommentService service) : ControllerBase
{
    private const int MaxFileSize = 500 * 1024; // 500 КБ — скорректируйте под требования
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/gif"];
    private static readonly string[] AllowedTextTypes = ["text/plain"];

    [HttpPost]
    [RequestSizeLimit(600 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] CommentCreateDto request,
        IFormFile? file,
        CancellationToken ct)
    {
        try
        {
            var upload = await PrepareFileAsync(file, ct);
            var dto = await service.CreateAsync(request, upload, ct);
            return CreatedAtAction(nameof(GetPage), new { }, dto);
        }
        catch (ValidationException ex) { return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) }); }
        catch (InvalidCaptchaException) { return BadRequest(new { error = "Неверный код CAPTCHA" }); }
        catch (FileValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sort = "createdAt",
        [FromQuery] string? dir = "desc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await service.GetRootPageAsync(page, pageSize, sort!, dir!, ct);
        return Ok(result);
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromForm] string text, CancellationToken ct)
    {
        var html = await service.PreviewAsync(text, ct);
        return Ok(new { html });
    }

    // ── Приватная часть: валидация и сохранение файла ──

    private async Task<FileUpload?> PrepareFileAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;

        if (file.Length > MaxFileSize)
            throw new FileValidationException("Файл превышает допустимый размер (500 КБ).");

        var isImage = AllowedImageTypes.Contains(file.ContentType);
        var isText = AllowedTextTypes.Contains(file.ContentType);
        if (!isImage && !isText)
            throw new FileValidationException("Разрешены только JPG/PNG/GIF до 500 КБ или TXT до 100 КБ.");

        if (isText && file.Length > 100 * 1024)
            throw new FileValidationException("TXT-файл превышает 100 КБ.");

        // Генерируем безопасное имя для хранения
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid():N}{ext}";

        // TODO: сохраняйте в постоянное хранилище (например wwwroot/uploads или S3)
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadsDir);

        await using var stream = System.IO.File.Create(Path.Combine(uploadsDir, storedName));
        await file.CopyToAsync(stream, ct);

        return new FileUpload(
            OriginalName: file.FileName,
            StoredName: storedName,
            MimeType: file.ContentType,
            Size: file.Length);
    }
}
