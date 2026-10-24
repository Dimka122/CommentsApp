using CommentsApp.Application.Interfaces;
using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Infrastructure.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<User> GetOrCreateAsync(
        string userName, string email, string? homepage, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            user = new User { UserName = userName.Trim(), Email = email, Homepage = homepage };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        else if (!string.Equals(user.UserName, userName.Trim(), StringComparison.Ordinal))
        {
            user.UserName = userName.Trim(); // пользователь может сменить имя
            await db.SaveChangesAsync(ct);
        }

        return user;
    }
}
