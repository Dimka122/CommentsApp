using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public sealed record UploadedFile(string FileName, long Length, Func<Stream> OpenRead);

public sealed record CreateCommentCommand(
    string? UserName, string? Email, string? Homepage, string? Text,
    Guid? ParentId, string? CaptchaId, string? CaptchaAnswer, UploadedFile? File);

public sealed record AttachmentDto(Guid Id, AttachmentKind Kind, string OriginalName, long SizeBytes);

public sealed record CommentDto(
    Guid Id, Guid? ParentId, string UserName, string Email, string? Homepage,
    string TextHtml, DateTime CreatedAt, List<AttachmentDto> Attachments)
{
    public List<CommentDto> Replies { get; init; } = new();
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public enum CommentSortBy { CreatedAt, UserName, Email }

public sealed record AttachmentFile(Stream Content, string ContentType);
