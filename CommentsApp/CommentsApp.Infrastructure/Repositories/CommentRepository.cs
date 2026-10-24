using AngleSharp.Css.Values;
using CommentsApp.Application.Interfaces;
using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CommentsApp.Infrastructure.Repositories;

public class CommentRepository(AppDbContext db) : ICommentRepository
{
    private static readonly Dictionary<string, Expression<Func<Comment, object>>>
        SortMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = c => c.CreatedAt,
            ["userName"] = c => c.User!.UserName,
            ["email"] = c => c.User!.Email,
        };

    public Task<int> CountRootAsync(CancellationToken ct = default) =>
        db.Comments.CountAsync(c => c.ParentCommentId == null, ct);

    public async Task<List<Comment>> GetRootPageAsync(
        int page, int pageSize, string sortField, string sortDir,
        CancellationToken ct = default)
    {
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        if (!SortMap.TryGetValue(sortField, out var key))
            throw new ArgumentException($"Unknown sort field: {sortField}");

        var query = ApplyIncludes(db.Comments.AsNoTracking())
            .Where(c => c.ParentCommentId == null);

        // ThenBy(Id) — тай-брейк, без него строки между страницами дублируются
        IOrderedQueryable<Comment> ordered = descending
            ? query.OrderByDescending(key).ThenByDescending(c => c.Id)
            : query.OrderBy(key).ThenBy(c => c.Id);

        return await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<List<Comment>> GetRepliesAsync(
        IEnumerable<int> parentIds, CancellationToken ct = default)
    {
        var ids = parentIds.ToList();
        if (ids.Count == 0) return [];

        return await ApplyIncludes(db.Comments.AsNoTracking())
            .Where(c => c.ParentCommentId != null && ids.Contains(c.ParentCommentId.Value))
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Comment comment, CancellationToken ct = default)
    {
        await db.Comments.AddAsync(comment, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    private static IQueryable<Comment> ApplyIncludes(IQueryable<Comment> q) =>
        q.Include(c => c.User)
         .Include(c => c.File);
}
