using Comments.Application.Comments;

namespace Comments.Application.Abstractions;

/// <param name="Key">Ключ с версией, зафиксированной ДО чтения из БД (защита от гонки с инвалидацией).</param>
public sealed record CacheLookup(string Key, PagedResult<CommentDto>? Value);

public interface ICommentCache
{
    Task<CacheLookup> LookupAsync(CommentSortBy sortBy, bool desc, int page, CancellationToken ct = default);
    Task StoreAsync(string key, PagedResult<CommentDto> value, CancellationToken ct = default);
    /// <summary>Мгновенно делает все закэшированные страницы неактуальными.</summary>
    Task InvalidateAsync(CancellationToken ct = default);
}
