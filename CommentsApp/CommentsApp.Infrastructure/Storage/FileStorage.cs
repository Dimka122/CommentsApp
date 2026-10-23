using CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Exceptions;
using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.Infrastructure.Services;

public class FileStorage : IFileStorage
{
    private readonly string _storageRoot;

    public FileStorage(IConfiguration config)
    {
        // путь из appsettings.json, напр. "FileStorage": { "Root": "uploads" }
        _storageRoot = config["FileStorage:Root"] ?? "uploads";

        if (!Directory.Exists(_storageRoot))
            Directory.CreateDirectory(_storageRoot);
    }

    public async Task<FileEntity?> SaveAsync(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var isImage = ext is ".jpg" or ".jpeg" or ".png" or ".gif";
        var maxBytes = isImage ? 5 * 1024 * 1024 : 100 * 1024;
        if (file.Length > maxBytes) throw new FileTooLargeException(ext);

        // защита: разрешаем только нужные типы
        if (!isImage && ext != ".txt") throw new UnsupportedFileTypeException(ext);

        var storedName = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(_storageRoot, storedName);
        await using (var stream = File.Create(path))
            await file.CopyToAsync(stream, ct);

        return new FileEntity
        {
            FileName = file.FileName,
            StoredName = storedName,
            MimeType = file.ContentType,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };
    }

    // метод для отдачи файла обратно (endpoint /api/files/{id})
    public Stream OpenRead(string storedName)
    {
        var path = Path.Combine(_storageRoot, storedName);
        return File.OpenRead(path);
    }
}
