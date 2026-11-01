using Comments.Application.Comments;
using HotChocolate;

namespace Comments.Api.GraphQL;

public sealed record CommentPage(IReadOnlyList<CommentDto> Items, int Page, int PageSize, int TotalCount);

public sealed class Query
{
    /// <summary>Корневые комментарии, по 25 на странице, сортировка CREATED_AT | USER_NAME | EMAIL (по умолчанию LIFO).</summary>
    public async Task<CommentPage> GetComments(
        [Service] CommentService comments,
        CommentSortBy sortBy = CommentSortBy.CreatedAt, bool desc = true, int page = 1,
        CancellationToken ct = default)
    {
        var r = await comments.GetPageAsync(sortBy, desc, page, ct);
        return new CommentPage(r.Items, r.Page, r.PageSize, r.TotalCount);
    }
}
