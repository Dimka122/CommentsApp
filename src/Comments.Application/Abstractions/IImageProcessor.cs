namespace Comments.Application.Abstractions;

public sealed record ProcessedImage(byte[] Bytes, string Extension);

public interface IImageProcessor
{
    /// <summary>Проверяет, что это JPG/GIF/PNG, и пропорционально уменьшает до 320x240. null = невалидное изображение.</summary>
    Task<ProcessedImage?> ProcessAsync(byte[] data, CancellationToken ct = default);
}
