using CommentsApp.Application.DTOs;
using CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Dtos;
using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;
using CommentsApp.Domain.Entities;
using FluentValidation;

namespace CommentsApp.Application.Services;

public class CommentService(
    ICommentRepository comments,
    IUserService users,
    IHtmlSanitizerService sanitizer,
    IValidator<CommentCreateDto> validator)
{
    private static readonly string[] SortFields = ["createdAt", "userName", "email"];
    private static readonly string[] SortDirs = ["asc", "desc"];

    public async Task<PagedResult<CommentDto>> GetRootPageAsync(
        int page, int pageSize, string sortField, string sortDir, CancellationToken ct)
    {
        if (page < 1) page = 1;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;
        sortField = SortFields.Contains(sortField.ToLowerInvariant()) ? sortField : "createdAt";
        sortDir = SortDirs.Contains(sortDir.ToLowerInvariant()) ? sortDir : "desc";

        var total = await comments.CountRootAsync(ct);
        var roots = await comments.GetRootPageAsync(page, pageSize, sortField, sortDir, ct);

        var level2 = await comments.GetRepliesAsync(roots.Select(r => r.Id), ct);
        var level3 = level2.Count > 0
            ? await comments.GetRepliesAsync(level2.Select(r => r.Id), ct)
            : [];

        var children = level2.Concat(level3)
            .GroupBy(r => r.ParentCommentId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ToDto).ToList());

        var items = roots.Select(r =>
        {
            var dto = ToDto(r);
            Attach(dto, children);
            return dto;
        }).ToList();

        return new PagedResult<CommentDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<string> PreviewAsync(string text, CancellationToken ct)
    {
        return await Task.Run(() => sanitizer.Sanitize(text), ct);
    }

    private static void Attach(CommentDto parent, Dictionary<int, List<CommentDto>> children)
    {
        if (!children.TryGetValue(parent.Id, out var list)) return;
        foreach (var child in list)
        {
            Attach(child, children);
            parent.Replies.Add(child);
        }
    }

    public async Task<CommentDto> CreateAsync(
        CommentCreateDto dto, FileUpload? file, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await users.GetOrCreateAsync(dto.UserName, dto.Email, dto.Homepage, ct);

        var comment = new Comment
        {
            UserId = user.Id,
            ParentCommentId = dto.ParentCommentId,
            Text = sanitizer.Sanitize(dto.Text),   // XSS: чистим до сохранения
            CreatedAt = DateTime.UtcNow,
        };

        if (file is not null)
            comment.File = new FileEntity
            {
                FileName = file.OriginalName,
                StoredName = file.StoredName,
                MimeType = file.MimeType,
                Size = file.Size,
            };

        await comments.AddAsync(comment, ct);
        await comments.SaveChangesAsync(ct);

        return ToDto(comment);
    }

    private static CommentDto ToDto(Comment c) => new()
    {
        Id = c.Id,
        UserName = c.User.UserName,
        Email = c.User.Email,
        Homepage = c.User.Homepage,
        Text = c.Text,
        CreatedAt = c.CreatedAt,
        File = c.File is null ? null : new FileDto
        {
            FileName = c.File.FileName,
            Url = $"/api/files/{c.File.StoredName}",
            Size = c.File.Size,
        },
    };
}

public record FileUpload(string OriginalName, string StoredName, string MimeType, long Size);
