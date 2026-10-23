using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Domain.Entities;
using CommentsApp.CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Infrastructure.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    private const string DefaultSort = "CreatedAt";

    public Task<int> CountRootAsync(CancellationToken ct = default) =>
        db.Comments.CountAsync(c => c.ParentCommentId == null, ct);

    public async Task<List<Comment>> GetRootPageAsync(
        int page, int pageSize, string sortField, string sortDir, CancellationToken ct = default)
    {
        var (field, dir) = Normalize(sortField, sortDir);

        var query = db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.File)
            .Where(c => c.ParentCommentId == null);

        query = (field, dir) switch
        {
            ("UserName", "asc") => query.OrderBy(c => c.User!.UserName),
            ("UserName", "desc") => query.OrderByDescending(c => c.User!.UserName),
            ("Email", "asc") => query.OrderBy(c => c.User!.Email),
            ("Email", "desc") => query.OrderByDescending(c => c.User!.Email),
            (_, "asc") => query.OrderBy(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt) // default LIFO
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Comment>> GetRepliesAsync(IEnumerable<int> rootIds, CancellationToken ct = default)
    {
        var ids = rootIds.ToArray();
        if (ids.Length == 0) return [];

        return await db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.File)
            .Where(c => c.ParentCommentId != null && ids.Contains(c.ParentCommentId.Value))
            .OrderBy(c => c.CreatedAt) // ответы — FIFO (старые сверху)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Comment comment, CancellationToken ct = default)
    {
        await db.Comments.AddAsync(comment, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    private static (string Field, string Dir) Normalize(string? field, string? dir)
    {
        var f = field?.ToLowerInvariant() switch
        {
            "username" or "user_name" or "name" => "UserName",
            "email" => "Email",
            _ => DefaultSort
        };
        var d = dir?.ToLowerInvariant() == "asc" ? "asc" : "desc";
        return (f, d);
    }
}
