using Comments.Application.Abstractions;
using SkiaSharp;

namespace Comments.Infrastructure.Imaging;

public sealed class SkiaImageProcessor : IImageProcessor
{
    public const int MaxWidth = 320, MaxHeight = 240;
    private const long MaxPixels = 25_000_000; // защита от "decompression bomb"

    public Task<ProcessedImage?> ProcessAsync(byte[] data, CancellationToken ct = default)
        => Task.FromResult(Process(data));

    private static ProcessedImage? Process(byte[] data)
    {
        try
        {
            using var skData = SKData.CreateCopy(data);
            using var codec = SKCodec.Create(skData);
            if (codec is null) return null;

            var format = codec.EncodedFormat;
            if (format is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Gif)) return null;
            if ((long)codec.Info.Width * codec.Info.Height > MaxPixels) return null;

            using var decoded = SKBitmap.Decode(codec);   // для GIF берётся первый кадр
            if (decoded is null) return null;
            using var oriented = Orient(decoded, codec.EncodedOrigin);

            var fits = oriented.Width <= MaxWidth && oriented.Height <= MaxHeight;
            // GIF, который уже укладывается в лимит, отдаём как есть (сохраняется анимация).
            if (format == SKEncodedImageFormat.Gif && fits) return new ProcessedImage(data, ".gif");

            SKBitmap? resized = null;
            try
            {
                var bmp = oriented;
                if (!fits)
                {
                    var scale = Math.Min((double)MaxWidth / oriented.Width, (double)MaxHeight / oriented.Height);
                    var w = Math.Max(1, (int)Math.Round(oriented.Width * scale));
                    var h = Math.Max(1, (int)Math.Round(oriented.Height * scale));
                    resized = oriented.Resize(new SKImageInfo(w, h), SKFilterQuality.High);
                    if (resized is null) return null;
                    bmp = resized;
                }

                // Перекодирование убирает метаданные (EXIF, в том числе геометки).
                var jpeg = format == SKEncodedImageFormat.Jpeg;
                using var image = SKImage.FromBitmap(bmp);
                using var encoded = image.Encode(jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, jpeg ? 85 : 100);
                return encoded is null ? null : new ProcessedImage(encoded.ToArray(), jpeg ? ".jpg" : ".png");
            }
            finally { resized?.Dispose(); }
        }
        catch (Exception)
        {
            return null; // повреждённый или подозрительный файл
        }
    }

    /// <summary>Учитывает EXIF-ориентацию (фото с телефона часто «лежат на боку»).</summary>
    private static SKBitmap Orient(SKBitmap src, SKEncodedOrigin origin)
    {
        int w = src.Width, h = src.Height;
        return origin switch
        {
            SKEncodedOrigin.BottomRight => Draw(src, w, h, c => { c.Translate(w, h); c.RotateDegrees(180); }),
            SKEncodedOrigin.RightTop => Draw(src, h, w, c => { c.Translate(h, 0); c.RotateDegrees(90); }),
            SKEncodedOrigin.LeftBottom => Draw(src, h, w, c => { c.Translate(0, w); c.RotateDegrees(270); }),
            _ => src,
        };
    }

    private static SKBitmap Draw(SKBitmap src, int width, int height, Action<SKCanvas> setup)
    {
        var dst = new SKBitmap(width, height);
        using var canvas = new SKCanvas(dst);
        canvas.Clear(SKColors.Transparent);
        setup(canvas);
        canvas.DrawBitmap(src, 0, 0);
        return dst;
    }
}