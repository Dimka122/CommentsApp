using Comments.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Captcha;

/// <summary>Заглушка для нагрузочного теста: ответ всегда "TEST". НИКОГДА не включать в проде.</summary>
public sealed class LoadTestCaptchaService : ICaptchaService
{
    private const string Pixel = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    public LoadTestCaptchaService(ILogger<LoadTestCaptchaService> log)
        => log.LogWarning("CAPTCHA is DISABLED (Captcha:Mode=LoadTest). Use only for load testing!");

    public Task<CaptchaChallenge> CreateAsync(CancellationToken ct = default)
        => Task.FromResult(new CaptchaChallenge(Guid.NewGuid().ToString("N"), Pixel));

    public Task<bool> ValidateAsync(string id, string answer, CancellationToken ct = default)
        => Task.FromResult(string.Equals(answer.Trim(), "TEST", StringComparison.OrdinalIgnoreCase));
}
