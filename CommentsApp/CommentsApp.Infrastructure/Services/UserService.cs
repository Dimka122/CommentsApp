using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;
using CommentsApp.CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.CommentsApp.Infrastructure.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<User> GetOrCreateAsync(
        string userName, string email, string? homepage, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = new User
            {
                UserName = userName,
                Email = email,
                Homepage = string.IsNullOrWhiteSpace(homepage) ? null : homepage,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
        }
        else
        {
            // обновляем имя/homepage, если клиент ввёл новые
            user.UserName = userName;
            if (!string.IsNullOrWhiteSpace(homepage))
                user.Homepage = homepage;
        }

        return user;
    }
}
