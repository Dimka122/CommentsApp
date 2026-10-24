using CommentsApp.Domain.Entities;

namespace CommentsApp.Application.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateAsync(string userName, string email, string? homepage, CancellationToken ct = default);
}
