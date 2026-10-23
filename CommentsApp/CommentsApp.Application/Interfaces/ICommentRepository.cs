using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.CommentsApp.Application.Interfaces
{
    public interface ICommentRepository
    {
        Task<int> CountRootAsync(CancellationToken ct = default);
        Task<List<Comment>> GetRootPageAsync(
            int page, int pageSize, string sortField, string sortDir,
            CancellationToken ct = default);
        Task<List<Comment>> GetRepliesAsync(IEnumerable<int> rootIds, CancellationToken ct = default);
        Task AddAsync(Comment comment, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
