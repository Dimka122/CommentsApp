namespace Comments.Application.Abstractions;

/// <summary>Хранилище файлов: сейчас volume на VDS, позже можно заменить на S3-совместимое.</summary>
public interface IFileStorage
{
    /// <summary>Сохраняет файл под сгенерированным именем и возвращает относительный путь.</summary>
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
}
