using System.Net.Mail;
using System.Text.RegularExpressions;
using Comments.Application.Common;

namespace Comments.Application.Comments;

public static partial class CommentValidator
{
    public const int MaxTextLength = 5000;

    [GeneratedRegex(@"^[A-Za-z0-9]{1,64}$")] private static partial Regex UserNameRx();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")] private static partial Regex EmailRx();
    [GeneratedRegex(@"^[A-Za-z0-9]{1,16}$")] private static partial Regex CaptchaRx();

    private static readonly string[] AllowedFileExtensions = [".jpg", ".jpeg", ".png", ".gif", ".txt"];

    public static Dictionary<string, List<string>> Validate(CreateCommentCommand c)
    {
        var e = new Dictionary<string, List<string>>();
        void Add(string key, string msg) { if (!e.TryGetValue(key, out var l)) e[key] = l = []; l.Add(msg); }

        if (string.IsNullOrWhiteSpace(c.UserName)) Add("userName", "User name is required.");
        else if (!UserNameRx().IsMatch(c.UserName.Trim())) Add("userName", "Use only Latin letters and digits (up to 64 characters).");

        var email = c.Email?.Trim();
        if (string.IsNullOrEmpty(email)) Add("email", "E-mail is required.");
        else if (email.Length > 254 || !EmailRx().IsMatch(email) || !MailAddress.TryCreate(email, out var addr) || addr.Address != email)
            Add("email", "E-mail format is invalid.");

        var home = c.Homepage?.Trim();
        if (!string.IsNullOrEmpty(home) &&
            (home.Length > 2048 || !Uri.TryCreate(home, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            Add("homepage", "Homepage must be a valid http(s) URL.");

        if (string.IsNullOrWhiteSpace(c.Text)) Add("text", "Text is required.");
        else if (c.Text.Length > MaxTextLength) Add("text", $"Text is too long (max {MaxTextLength} characters).");

        if (string.IsNullOrWhiteSpace(c.CaptchaId) || string.IsNullOrWhiteSpace(c.CaptchaAnswer)) Add("captchaAnswer", "CAPTCHA is required.");
        else if (!CaptchaRx().IsMatch(c.CaptchaAnswer.Trim())) Add("captchaAnswer", "CAPTCHA contains only Latin letters and digits.");

        if (c.File is not null)
        {
            var ext = Path.GetExtension(c.File.FileName).ToLowerInvariant();
            if (!AllowedFileExtensions.Contains(ext)) Add("file", "Allowed files: JPG, GIF, PNG images or TXT up to 100 KB.");
            else if (ext == ".txt" && c.File.Length > 100 * 1024) Add("file", "Text file must not exceed 100 KB.");
            else if (ext != ".txt" && c.File.Length > 5 * 1024 * 1024) Add("file", "Image must not exceed 5 MB.");
        }
        return e;
    }

    public static ValidationException ToException(Dictionary<string, List<string>> e)
        => new(e.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
}
