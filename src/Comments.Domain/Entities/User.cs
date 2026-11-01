namespace Comments.Domain.Entities;

/// <summary>Автор комментария + данные для идентификации клиента.</summary>
public class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Homepage { get; private set; }
    public string IpAddress { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public static User Create(string userName, string email, string? homepage, string ip, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("UserName is required", nameof(userName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
        return new User
        {
            Id = Guid.CreateVersion7(),
            UserName = userName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Homepage = string.IsNullOrWhiteSpace(homepage) ? null : homepage.Trim(),
            IpAddress = ip,
            UserAgent = userAgent.Length > 512 ? userAgent[..512] : userAgent,
            CreatedAt = DateTime.UtcNow
        };
    }
}
