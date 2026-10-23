using CommentsApp.CommentsApp.Application.Dtos;
using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.CommentsApp.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> GetOrCreateAsync(string userName, string email, string? homepage, CancellationToken ct = default);
    }
}
