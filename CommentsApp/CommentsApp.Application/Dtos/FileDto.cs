namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record FileDto(
    int Id,
    string FileName,
    string MimeType,
    long Size,
    string Url);

}
