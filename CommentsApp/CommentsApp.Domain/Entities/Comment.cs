namespace CommentsApp.CommentsApp.Domain.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Text { get; set; } = string.Empty; // уже санитизированный HTML
        public DateTime CreatedAt { get; set; }

        public int? ParentCommentId { get; set; }   // null = корневой
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();

        public FileEntity? File { get; set; }
    }
}
