namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record CommentDto(
    int Id,
    string UserName,
    string Email,
    string? Homepage,
    string Text,
    DateTime CreatedAt,
    int? ParentId,
    List<CommentDto> Replies,
    FileDto? File=null);
}
