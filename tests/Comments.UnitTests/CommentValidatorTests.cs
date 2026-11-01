using Comments.Application.Comments;
using Xunit;

namespace Comments.UnitTests;

public class CommentValidatorTests
{
    private static CreateCommentCommand Valid() =>
        new("John123", "john@example.com", null, "hello", null, "abc", "A1B2C", null);

    [Fact] public void Valid_command_has_no_errors() => Assert.Empty(CommentValidator.Validate(Valid()));

    [Theory]
    [InlineData("Иван")] [InlineData("john doe")] [InlineData("")] [InlineData("a_b")]
    public void Bad_user_names(string name) =>
        Assert.True(CommentValidator.Validate(Valid() with { UserName = name }).ContainsKey("userName"));

    [Theory] [InlineData("nope")] [InlineData("a@b")] [InlineData("a b@c.com")]
    public void Bad_emails(string email) =>
        Assert.True(CommentValidator.Validate(Valid() with { Email = email }).ContainsKey("email"));

    [Fact] public void Homepage_is_optional_but_must_be_url_when_set()
    {
        Assert.Empty(CommentValidator.Validate(Valid() with { Homepage = "" }));
        Assert.Empty(CommentValidator.Validate(Valid() with { Homepage = "https://example.com" }));
        Assert.True(CommentValidator.Validate(Valid() with { Homepage = "javascript:alert(1)" }).ContainsKey("homepage"));
    }

    [Fact] public void Captcha_is_required() =>
        Assert.True(CommentValidator.Validate(Valid() with { CaptchaAnswer = "" }).ContainsKey("captchaAnswer"));

    [Fact] public void Large_text_file_rejected() =>
        Assert.True(CommentValidator.Validate(Valid() with { File = new("a.txt", 101 * 1024, () => Stream.Null) }).ContainsKey("file"));
}
