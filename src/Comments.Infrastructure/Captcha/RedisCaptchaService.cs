using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Comments.Application.Abstractions;
using SkiaSharp;
using StackExchange.Redis;

namespace Comments.Infrastructure.Captcha;

public sealed partial class RedisCaptchaService(IConnectionMultiplexer redis) : ICaptchaService
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // без похожих символов 0/O, 1/I
    private const int Length = 5;
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    [GeneratedRegex("^[0-9a-f]{32}$")] private static partial Regex IdRx();

    public async Task<CaptchaChallenge> CreateAsync(CancellationToken ct = default)
    {
        var code = new string(Enumerable.Range(0, Length).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]).ToArray());
        var id = Guid.NewGuid().ToString("N");
        await redis.GetDatabase().StringSetAsync($"captcha:{id}", code.ToLowerInvariant(), Ttl);
        return new CaptchaChallenge(id, "data:image/png;base64," + Convert.ToBase64String(Render(code)));
    }

    public async Task<bool> ValidateAsync(string id, string answer, CancellationToken ct = default)
    {
        if (!IdRx().IsMatch(id)) return false;
        var stored = await redis.GetDatabase().StringGetDeleteAsync($"captcha:{id}");
        return stored.HasValue && string.Equals(stored.ToString(), answer.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }

    private static byte[] Render(string code)
    {
        const int W = 170, H = 60;
        var rnd = Random.Shared;
        SKColor Rnd(int lo, int hi) => new((byte)rnd.Next(lo, hi), (byte)rnd.Next(lo, hi), (byte)rnd.Next(lo, hi));

        using var bmp = new SKBitmap(W, H);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(new SKColor(245, 247, 250));

        using var noise = new SKPaint { IsAntialias = true, StrokeWidth = 2 };
        for (var i = 0; i < 7; i++)
        {
            noise.Color = Rnd(120, 220);
            canvas.DrawLine(rnd.Next(W), rnd.Next(H), rnd.Next(W), rnd.Next(H), noise);
        }

        using var typeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, 34);
        using var paint = new SKPaint { IsAntialias = true };
        var x = 14f;
        foreach (var ch in code)
        {
            canvas.Save();
            canvas.RotateDegrees(rnd.Next(-18, 19), x + 12, H / 2f);
            paint.Color = Rnd(20, 110);
            canvas.DrawText(ch.ToString(), x, 40 + rnd.Next(-6, 7), font, paint);
            canvas.Restore();
            x += 30;
        }

        for (var i = 0; i < 80; i++)
        {
            noise.Color = Rnd(100, 200);
            canvas.DrawPoint(rnd.Next(W), rnd.Next(H), noise);
        }

        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
