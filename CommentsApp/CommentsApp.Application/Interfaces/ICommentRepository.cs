using CommentsApp.Domain.Entities;

namespace CommentsApp.Application.Interfaces;

public interface ICommentRepository
{
    Task<int> CountRootAsync(CancellationToken ct = default);
    Task<List<Comment>> GetRootPageAsync(
        int page, int pageSize, string sortField, string sortDir,
        CancellationToken ct = default);
    Task<List<Comment>> GetRepliesAsync(IEnumerable<int> parentIds, CancellationToken ct = default);
    Task AddAsync(Comment comment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

}
