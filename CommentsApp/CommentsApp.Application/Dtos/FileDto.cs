namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record FileDto(
    Guid Id,
    string FileName,
    string MimeType,
    long SizeBytes);
}
