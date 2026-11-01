using Comments.Application.Abstractions;
using Comments.Application.Search;
using Comments.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Comments.Worker;

/// <summary>Индексирует новый комментарий в Elasticsearch. Идемпотентно; при ошибке MassTransit повторяет доставку.</summary>
public sealed class CommentIndexConsumer(IAppDbContext db, ICommentSearch search, ILogger<CommentIndexConsumer> log)
    : IConsumer<CommentCreated>
{
    public async Task Consume(ConsumeContext<CommentCreated> context)
    {
        var id = context.Message.CommentId;
        var doc = await db.Comments.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CommentSearchDoc(c.Id, c.ParentId, c.User.UserName, c.User.Email, c.TextHtml, c.CreatedAt))
            .FirstOrDefaultAsync(context.CancellationToken);

        if (doc is null) { log.LogWarning("Comment {CommentId} not found; skipping indexing", id); return; }

        await search.IndexAsync(doc with { Text = PlainText.FromHtml(doc.Text) }, context.CancellationToken);
        log.LogInformation("Indexed comment {CommentId}", id);
    }
}
