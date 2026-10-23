namespace CommentsApp.CommentsApp.Application.Interfaces
{
    public interface ICaptchaService
    {
        CaptchaChallenge Generate();
        bool Validate(string token, string code);
    }

    public record CaptchaChallenge(string Token, string ImageBase64);
}
