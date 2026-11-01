namespace Comments.Api.Contracts;

public sealed class CreateCommentForm
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Homepage { get; set; }
    public string? Text { get; set; }
    public Guid? ParentId { get; set; }
    public string? CaptchaId { get; set; }
    public string? CaptchaAnswer { get; set; }
    public IFormFile? File { get; set; }
}

public sealed record PreviewRequest(string? Text);
public sealed record PreviewResponse(string Html);
