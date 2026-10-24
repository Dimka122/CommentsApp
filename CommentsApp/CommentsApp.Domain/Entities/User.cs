using CommentsApp.CommentsApp.Domain.Entities;

namespace CommentsApp.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Homepage { get; set; }

    public List<Comment> Comments { get; set; } = [];
}
