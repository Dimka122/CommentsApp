namespace Comments.Domain.Entities;

public enum AttachmentKind { Image = 1, Text = 2 }

public class Attachment
{
    private Attachment() { }

    public Guid Id { get; private set; }
    public Guid CommentId { get; private set; }
    public AttachmentKind Kind { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public string OriginalName { get; private set; } = null!;
    public long SizeBytes { get; private set; }

    public static Attachment Create(Guid commentId, AttachmentKind kind, string storagePath, string originalName, long size)
        => new()
        {
            Id = Guid.CreateVersion7(),
            CommentId = commentId,
            Kind = kind,
            StoragePath = storagePath,
            OriginalName = originalName,
            SizeBytes = size
        };
}
