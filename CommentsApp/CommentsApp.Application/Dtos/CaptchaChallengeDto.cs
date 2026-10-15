namespace CommentsApp.CommentsApp.Application.Dtos
{
    public record CaptchaChallengeDto(
    string Token,
    string ImageBase64);
}
