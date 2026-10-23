using AngleSharp.Css.Dom;
using CommentsApp.CommentsApp.Application.Interfaces;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CommentsApp.CommentsApp.Infrastructure.Captcha;

public class CaptchaService : ICaptchaService
{
    private const int TtlMinutes = 5;
    private readonly ConcurrentDictionary<string, (string Code, DateTime Expire)> _store = new();
    // Для масштабирования выносим в Redis/кэш — сейчас in-memory для простоты.

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

    private static string DrawCaptcha(string code)
    {
        const int width = 180, height = 60;
        using var image = new Image<Rgba32>(width, height);
        var random = Random.Shared;

        image.Mutate(ctx =>
        {
            // фон
            ctx.Fill(Color.FromRgb(240, 240, 245));

            // шумовые линии
            for (var i = 0; i < 6; i++)
            {
                var pen = Pens.Solid(Color.FromRgb(
                    (byte)random.Next(120, 220),
                    (byte)random.Next(120, 220),
                    (byte)random.Next(120, 220)), 1.5f);
                ctx.DrawLine(pen,
                    new PointF(random.Next(width), random.Next(height)),
                    new PointF(random.Next(width), random.Next(height)));
            }

            // символы
            var font = SystemFonts.CreateFont("Arial", 28, FontStyle.Bold);
            for (var i = 0; i < code.Length; i++)
            {
                var color = Color.FromRgb(
                    (byte)random.Next(0, 120),
                    (byte)random.Next(0, 120),
                    (byte)random.Next(0, 120));
                ctx.DrawText(code[i].ToString(), font, color,
                    new PointF(15 + i * 26, random.Next(5, 18)));
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return Convert.ToBase64String(ms.ToArray());
    }
}