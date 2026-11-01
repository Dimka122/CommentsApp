namespace Comments.Application.Abstractions;

public sealed record CaptchaChallenge(string Id, string ImageDataUri);

public interface ICaptchaService
{
    Task<CaptchaChallenge> CreateAsync(CancellationToken ct = default);
    /// <summary>Одноразовая проверка: ответ удаляется при первой же попытке.</summary>
    Task<bool> ValidateAsync(string id, string answer, CancellationToken ct = default);
}
