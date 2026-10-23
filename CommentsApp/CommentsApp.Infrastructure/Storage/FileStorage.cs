using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace CommentsApp.CommentsApp.Infrastructure.Storage;

public class FileStorage(IWebHostEnvironment env) : IFileStorage
{
    private static readonly Dictionary<string, string> AllowedImages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif"
    };
    private const int MaxImageBytes = 500 * 1024;   // 500 КБ
    private const int* 1024;     // 100 КБ
    private const int MaxWidth = 320;
    private const int MaxHeight = 240;

    public async Task<FileEntity?> SaveAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return null;

        var mime = file.ContentType.ToLowerInvariant();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (AllowedImages.TryGetValue(mime, out var expectedExt))
        {
            if (file.Length > MaxImageBytes)
                throw new FileValidationException("Картинка больше 500 КБ");

            // Проверяем реальный формат по содержимому (не доверяем Content-Type)
            using var img = await Image.LoadAsync(file.OpenReadStream(), ct);

            var (newW, newH) = ResizeKeepAspect(img.Width, img.Height, MaxWidth, MaxHeight);
            img.Mutate(x => x.Resize(newW, newH));

            var storedName = $"{Guid.NewGuid():N}{expectedExt}";
            var dir = GetDir();
            await img.SaveAsync(Path.Combine(dir, storedName), ct);

            return new FileEntity
            {
                FileName = file.FileName,
                StoredName = storedName,
                MimeType = mime,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };
        }

        if (mime == "text/plain" && ext == ".txt")
        {
            if (file.Length > MaxTxtBytes)
                throw new FileValidationException("TXT-файл больше 100 КБ");

            var storedName = $"{Guid.NewGuid():N}.txt";
            var dir = GetDir();
            await using var stream = System.IO.File.Create(Path.Combine(dir, storedName));
            await file.CopyToAsync(stream, ct);

            return new FileEntity
            {
                FileName = file.FileName,
                StoredName = storedName,
                MimeType = mime,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };
        }

        throw new FileValidationException(
            "Допустимы только картинки (JPG/PNG/GIF ≤500 КБ) или TXT ≤100 КБ");
    }

    public FileStream? Open(string storedName)
    {
        var path = Path.Combine(GetDir(), storedName);
        return System.IO.File.Exists(path) ? System.IO.File.OpenRead(path) : null;
    }

    private static string GetDir() =>
        Path.Combine(Directory.GetCurrentDirectory(), "AppData", "uploads");
    // mkdir при первом обращении:
    // Directory.CreateDirectory в конструкторе или здесь

    private static (int W, int H) ResizeKeepAspect(int w, int h, int maxW, int maxH)
    {
        var scale = Math.Min((double)maxW / w, (double)maxH / h);
        if (scale >= 1) return (w, h); // не увеличиваем
        return ((int)Math.Round(w * scale), (int)Math.Round(h * scale));
    }
}

public class FileValidationException(string message) : Exception(message);

