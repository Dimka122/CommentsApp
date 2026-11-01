using Comments.Domain;

namespace Comments.Application.Abstractions;

public interface ICommentEventPublisher
{
    Task PublishAsync(CommentCreated e, CancellationToken ct = default);
}
