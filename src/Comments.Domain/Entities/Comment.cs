namespace Comments.Domain.Entities;

public class Comment
{
    private readonly List<Comment> _replies = new();
    private readonly List<Attachment> _attachments = new();

    private Comment() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public Comment? Parent { get; private set; }
    /// <summary>Исходный текст, как ввёл пользователь.</summary>
    public string Text { get; private set; } = null!;
    /// <summary>Санитизированный HTML (разрешены только a, code, i, strong).</summary>
    public string TextHtml { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Comment> Replies => _replies;
    public IReadOnlyCollection<Attachment> Attachments => _attachments;

    public bool IsRoot => ParentId is null;

    public static Comment Create(User author, Guid? parentId, string text, string sanitizedHtml)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required", nameof(text));
        return new Comment
        {
            Id = Guid.CreateVersion7(),
            User = author,
            UserId = author.Id,
            ParentId = parentId,
            Text = text,
            TextHtml = sanitizedHtml,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddAttachment(Attachment attachment) => _attachments.Add(attachment);
}
