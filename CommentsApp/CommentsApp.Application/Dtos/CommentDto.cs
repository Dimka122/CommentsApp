namespace CommentsApp.CommentsApp.Application.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Homepage { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public FileDto? File { get; set; }
        public List<CommentDto> Replies { get; set; } = [];
    }
}
