using Comments.Application.Comments;
using Comments.Domain;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

public sealed class CommentBroadcastConsumer(CommentService comments, IHubContext<CommentsHub> hub) : IConsumer<CommentCreated>
{
    public async Task Consume(ConsumeContext<CommentCreated> context)
    {
        var dto = await comments.GetByIdAsync(context.Message.CommentId, context.CancellationToken);
        if (dto is null) return;
        await hub.Clients.All.SendAsync("commentCreated", dto, context.CancellationToken);
    }
}
