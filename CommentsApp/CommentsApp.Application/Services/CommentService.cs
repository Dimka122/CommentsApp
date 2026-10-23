using CommentsApp.CommentsApp.Application.Dtos;
using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;
using FluentValidation;

namespace CommentsApp.CommentsApp.Application.Services;

public class CommentService(
    ICommentRepository comments,
    IUserService users,
    IHtmlSanitizerService sanitizer,
    ICaptchaService captcha,
    IFileStorage storage,
    CreateCommentValidator validator)
{
    public async Task<CommentDto> CreateAsync(CreateCommentRequest req, IFormFile? file, CancellationToken ct)
    {
        // 1. Валидация DTO
        var validationResult = await validator.ValidateAsync(req, ct);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // 2. CAPTCHA
        if (!captcha.Validate(req.CaptchaToken, req.CaptchaCode))
            throw new InvalidCaptchaException();

        // 3. Санитизация текста (XSS)
        var safeText = sanitizer.Sanitize(req.Text);

        // 4. Идентификация пользователя
        var user = await users.GetOrCreateAsync(req.UserName, req.Email, req.Homepage, ct);

        // 5. Файл (картинка ИЛИ txt)
        var fileEntity = await storage.SaveAsync(file, ct);

        // 6. Сохранение
        var comment = new Comment
        {
            UserId = user.Id,
            Text = safeText,
            CreatedAt = DateTime.UtcNow,
            File = fileEntity
        };
        if (fileEntity is not null) fileEntity.Comment = comment;

        await comments.AddAsync(comment, ct);
        await comments.SaveChangesAsync(ct);

        return ToDto(comment);
    }

    public async Task<PagedResult<CommentDto>> GetRootPageAsync(
        int page, int pageSize, string sortField, string sortDir, CancellationToken ct)
    {
        var total = await comments.CountRootAsync(ct);
        var roots = await comments.GetRootPageAsync(page, pageSize, sortField, sortDir, ct);
        var replies = await comments.GetRepliesAsync(roots.Select(r => r.Id), ct);

        var lookup = replies
            .GroupBy(r => r.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(ToDto).ToList());

        var items = roots.Select(r =>
        {
            var dto = ToDto(r);
            dto.Replies.AddRange(lookup.GetValueOrDefault(r.Id, []));
            return dto;
        }).ToList();

        return new PagedResult<CommentDto>(items, page, pageSize, total);
    }

    public string Preview(string rawText) => sanitizer.Sanitize(rawText);

    private static CommentDto ToDto(Comment c) => new(
        c.Id,
        c.User.UserName,
        c.User.Email,
        c.User.Homepage,
        c.Text,
        c.CreatedAt,
        c.File is null ? null : new FileDto(c.File.Id, c.File.FileName, $"/api/files/{c.File.Id}", c.File.MimeType),
        []);
}

public class InvalidCaptchaException : Exception
{
    public InvalidCaptchaException() : base("Неверный код CAPTCHA") { }
}

