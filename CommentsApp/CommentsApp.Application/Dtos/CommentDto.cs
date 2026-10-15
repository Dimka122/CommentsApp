namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record CommentDto(
    Guid Id,
    string UserName,
    string Email,
    string? Homepage,
    string Text,
    DateTime CreatedAt,
    Guid? ParentId,
    IEnumerable<CommentDto> Replies,
    FileDto? File);
}
