using Comments.Application.Abstractions;
using Comments.Domain;

namespace Comments.Infrastructure.Events;

/// <summary>Заглушка. На шаге 4 заменяется публикацией в RabbitMQ (MassTransit).</summary>
public sealed class NullCommentEventPublisher : ICommentEventPublisher
{
    public Task PublishAsync(CommentCreated e, CancellationToken ct = default) => Task.CompletedTask;
}
