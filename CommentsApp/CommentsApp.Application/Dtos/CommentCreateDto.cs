namespace CommentsApp.CommentsApp.Application.Dtos;

public class CommentCreateDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Homepage { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? ParentId { get; set; }        
    public string CaptchaToken { get; set; } = string.Empty;
    public string CaptchaCode { get; set; } = string.Empty;
}
