using CommentsApp.CommentsApp.Application.Interfaces;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CommentsApp.CommentsApp.Infrastructure.Captcha;

public class CaptchaService : ICaptchaService
{
    private const int TtlMinutes = 5;
    private readonly ConcurrentDictionary<string, (string Code, DateTime Expire)> _store = new();
    // Для масштабирования выносим в Redis/кэш — сейчас in-memory для простоты.

    private static readonly SKTypeface CaptchaTypeface = LoadTypeface();

    public CaptchaChallenge Generate()
    {
        var code = GenerateCode(6);
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        _store[token] = (code.ToLowerInvariant(), DateTime.UtcNow.AddMinutes(TtlMinutes));

        var image = DrawCaptcha(code);
        return new CaptchaChallenge(token, image);
    }

    public bool Validate(string token, string code)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(code))
            return false;

        if (!_store.TryRemove(token, out var entry)) // одноразовость
            return false;

        if (DateTime.UtcNow > entry.Expire)
            return false;

        return string.Equals(entry.Code, code.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    private static string GenerateCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // без похожих символов
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
    }

    private static SKTypeface LoadTypeface()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Roboto-Bold.ttf");

        if (!File.Exists(path))
            throw new FileNotFoundException(
                "Шрифт капчи не найден. Убедитесь, что Assets/Roboto-Bold.ttf копируется в вывод сборки.",
                path);

        return SKTypeface.FromFile(path)
            ?? throw new InvalidOperationException($"Не удалось загрузить шрифт: {path}");
    }

    private static string DrawCaptcha(string code)
    {
        const int width = 180, height = 60;
        var random = Random.Shared;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // фон
        canvas.Clear(new SKColor(240, 240, 245));

        // шумовые линии
        for (var i = 0; i < 6; i++)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(
                    (byte)random.Next(120, 220),
                    (byte)random.Next(120, 220),
                    (byte)random.Next(120, 220)),
                StrokeWidth = 1.5f,
                IsAntialias = true
            };
            canvas.DrawLine(
                random.Next(width), random.Next(height),
                random.Next(width), random.Next(height),
                paint);
        }

        // символы
        using var font = new SKFont(CaptchaTypeface, 28);
        for (var i = 0; i < code.Length; i++)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(
                    (byte)random.Next(0, 120),
                    (byte)random.Next(0, 120),
                    (byte)random.Next(0, 120)),
                IsAntialias = true
            };
            canvas.DrawText(
                code[i].ToString(),
                15 + i * 26,
                random.Next(35, 50), // в Skia Y — это базовая линия текста
                SKTextAlign.Left,
                font,
                paint);
        }

        using var snapshot = surface.Snapshot();
        using var data = snapshot.Encode(SKEncodedImageFormat.Png, 90);
        return Convert.ToBase64String(data.ToArray());
    }
}
