namespace CommentsApp.Application.DTOs;

public class CommentCreateDto
{
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Homepage { get; set; }
    public string Text { get; set; } = "";
    public int? ParentCommentId { get; set; }
    public string CaptchaToken { get; set; } = "";
    public string CaptchaId { get; set; } = "";

}
