using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.Application.Interfaces;

public interface IFileStorage
{
    Task<FileEntity?> SaveAsync(IFormFile? file, CancellationToken ct);
    Stream OpenRead(string storedName);
}
