using System.Text.Json;
using Comments.Application.Comments;
using Comments.Domain.Entities;
using Xunit;

namespace Comments.UnitTests;

public class CommentTreeTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static CommentDto C(string name, int minutes, Guid? parent = null, params AttachmentDto[] att) =>
        new(Guid.NewGuid(), parent, name, name + "@e.com", null, "<i>" + name + "</i>", T0.AddMinutes(minutes), att.ToList());

    [Fact]
    public void Descendants_are_depth_first_oldest_first()
    {
        var root = C("root", 0);
        var a = C("a", 1, root.Id); var b = C("b", 3, root.Id); var a1 = C("a1", 2, a.Id);
        root.Replies.Add(b); root.Replies.Add(a);   // порядок добавления намеренно «неправильный»
        a.Replies.Add(a1);

        Assert.Equal(["a", "a1", "b"], CommentTree.Descendants(root).Select(x => x.UserName));
    }

    [Fact]
    public void Descendants_of_leaf_is_empty() => Assert.Empty(CommentTree.Descendants(C("x", 0)));

    [Fact]
    public void Page_survives_json_round_trip_for_cache()
    {
        var root = C("root", 0, null, new AttachmentDto(Guid.NewGuid(), AttachmentKind.Image, "cat.png", 123));
        root.Replies.Add(C("reply", 1, root.Id));
        var page = new PagedResult<CommentDto>([root], 1, 25, 1);

        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var back = JsonSerializer.Deserialize<PagedResult<CommentDto>>(JsonSerializer.Serialize(page, opts), opts)!;

        Assert.Equal(1, back.TotalCount);
        var r = Assert.Single(back.Items);
        Assert.Equal("root", r.UserName);
        Assert.Equal(AttachmentKind.Image, Assert.Single(r.Attachments).Kind);
        Assert.Equal("reply", Assert.Single(r.Replies).UserName);
    }
}
