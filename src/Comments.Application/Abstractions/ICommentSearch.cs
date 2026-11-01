namespace Comments.Application.Abstractions;

public sealed record CommentSearchDoc(Guid Id, Guid? ParentId, string UserName, string Email, string Text, DateTime CreatedAt);

/// <param name="Snippet">Фрагмент текста; HTML-экранирован, подсветка совпадений — только теги &lt;mark&gt;, поэтому безопасен для вставки в страницу.</param>
public sealed record SearchHit(Guid Id, Guid? ParentId, string UserName, string Snippet, DateTime CreatedAt);

public sealed record SearchPage(IReadOnlyList<SearchHit> Items, int Page, int PageSize, int TotalCount);

public interface ICommentSearch
{
    Task IndexAsync(CommentSearchDoc doc, CancellationToken ct = default);
    Task<SearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default);
}
