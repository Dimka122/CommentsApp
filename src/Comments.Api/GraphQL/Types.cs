using Comments.Application.Comments;
using HotChocolate.Types;

namespace Comments.Api.GraphQL;

public sealed class CommentGqlType : ObjectType<CommentDto>
{
    protected override void Configure(IObjectTypeDescriptor<CommentDto> d)
    {
        d.Name("Comment");
        // replies — прямые ответы; descendants — все вложенные ответы плоским списком (по ParentId клиент строит дерево).
        // Оба поля берутся из уже загруженного (и закэшированного) дерева, дополнительных запросов к БД нет.
        d.Field("descendants")
            .Type<NonNullType<ListType<NonNullType<CommentGqlType>>>>()
            .Resolve(ctx => CommentTree.Descendants(ctx.Parent<CommentDto>()));
    }
}

public sealed class AttachmentGqlType : ObjectType<AttachmentDto>
{
    protected override void Configure(IObjectTypeDescriptor<AttachmentDto> d) => d.Name("Attachment");
}
