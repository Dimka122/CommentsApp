namespace CommentsApp.CommentsApp.Domain.Entities
{
    public class FileEntity
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;   // оригинальное имя
        public string StoredName { get; set; } = string.Empty; // имя на диске
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
