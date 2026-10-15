namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record UploadedFile(string FileName, string ContentType, long Length, Stream Content);
}
