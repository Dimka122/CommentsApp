using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.CommentsApp.Application.Interfaces
{
    public interface IFileStorage
    {
        Task<FileEntity?> SaveAsync(IFormFile? file, CancellationToken ct = default);
        FileStream? Open(string storedName);
    }
}
