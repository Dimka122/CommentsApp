using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comments.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Attachment> Attachments { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
