namespace Comments.Application.Comments;

public static class CommentTree
{
    /// <summary>Все вложенные ответы плоским списком: обход в глубину, внутри уровня — от старых к новым.
    /// Клиент восстанавливает дерево по ParentId.</summary>
    public static IReadOnlyList<CommentDto> Descendants(CommentDto root)
    {
        var result = new List<CommentDto>();
        var stack = new Stack<CommentDto>();
        foreach (var r in root.Replies.OrderByDescending(x => x.CreatedAt)) stack.Push(r);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            result.Add(node);
            foreach (var r in node.Replies.OrderByDescending(x => x.CreatedAt)) stack.Push(r);
        }
        return result;
    }
}
