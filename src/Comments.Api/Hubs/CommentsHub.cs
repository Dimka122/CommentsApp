using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

/// <summary>Только сервер -> клиент. Событие "commentCreated" приходит с полным CommentDto (без вложенных ответов).</summary>
public sealed class CommentsHub : Hub;
