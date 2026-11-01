using Comments.Application.Abstractions;
using Comments.Domain;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Messaging;

/// <summary>
/// Публикует CommentCreated в RabbitMQ. Комментарий уже сохранён, поэтому недоступность брокера
/// не должна ломать запрос: ограничиваем ожидание 3 секундами и логируем ошибку.
/// (Гарантированную доставку даст transactional outbox — запланировано для Middle+.)
/// </summary>
public sealed class MassTransitCommentEventPublisher(IPublishEndpoint publish, ILogger<MassTransitCommentEventPublisher> log)
    : ICommentEventPublisher
{
    public async Task PublishAsync(CommentCreated e, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(3));
        try { await publish.Publish(e, cts.Token); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to publish CommentCreated {CommentId}", e.CommentId); }
    }
}
