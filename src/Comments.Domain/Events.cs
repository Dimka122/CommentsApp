namespace Comments.Domain;

/// <summary>Интеграционное событие: публикуется в RabbitMQ после сохранения комментария.</summary>
public record CommentCreated(Guid CommentId, Guid? ParentId, Guid UserId, DateTime CreatedAt);
