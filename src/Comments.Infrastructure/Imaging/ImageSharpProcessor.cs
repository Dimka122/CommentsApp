using Comments.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace Comments.Infrastructure.Imaging;

public sealed class ImageSharpProcessor : IImageProcessor
{
    public const int MaxWidth = 320, MaxHeight = 240;
    private const long MaxPixels = 25_000_000; // защита от "decompression bomb"

    public async Task<ProcessedImage?> ProcessAsync(byte[] data, CancellationToken ct = default)
    {
        try
        {
            using var input = new MemoryStream(data);
            var info = await Image.IdentifyAsync(input, ct);
            if (info is null || (long)info.Width * info.Height > MaxPixels) return null;

            input.Position = 0;
            using var image = await Image.LoadAsync(input, ct);
            var format = image.Metadata.DecodedImageFormat;
            var ext = format?.Name switch { "JPEG" => ".jpg", "PNG" => ".png", "GIF" => ".gif", _ => null };
            if (format is null || ext is null) return null;

            if (image.Width > MaxWidth || image.Height > MaxHeight)
                image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(MaxWidth, MaxHeight), Mode = ResizeMode.Max }));

            image.Metadata.ExifProfile = null; // убираем EXIF (в т.ч. геометки)

            using var output = new MemoryStream();
            await image.SaveAsync(output, image.Configuration.ImageFormatsManager.GetEncoder(format), ct);
            return new ProcessedImage(output.ToArray(), ext);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return null;
        }
    }
}
