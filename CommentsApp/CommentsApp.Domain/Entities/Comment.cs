using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.Domain.Entities;

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public List<Comment> Replies { get; set; } = [];
    public FileEntity? File { get; set; }
}
