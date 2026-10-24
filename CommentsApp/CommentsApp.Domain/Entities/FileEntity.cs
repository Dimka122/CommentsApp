using CommentsApp.Domain.Entities;

namespace CommentsApp.CommentsApp.Domain.Entities;

public class FileEntity
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public string FileName { get; set; } = null!;
    public string StoredName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long Size { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Comment Comment { get; set; } = null!;
}
