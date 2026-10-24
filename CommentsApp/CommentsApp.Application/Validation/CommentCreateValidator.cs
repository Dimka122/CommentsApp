using System.Text.RegularExpressions;
using FluentValidation;
using CommentsApp.Application.DTOs;

namespace CommentsApp.Application.Validation;

public partial class CommentCreateValidator : AbstractValidator<CommentCreateDto>
{
    private static readonly string[] AllowedTags =
        ["a", "i", "b", "strong", "em", "code", "pre"];

    public CommentCreateValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().MaximumLength(254);

        RuleFor(x => x.Homepage)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Homepage))
            .WithMessage("Homepage must be a valid absolute URL.")
            .MaximumLength(500);

        RuleFor(x => x.Text)
            .NotEmpty().MaximumLength(5000);

        RuleFor(x => x.CaptchaToken)
            .NotEmpty();
    }

    private static bool BeValidUrl(string? url) =>
        url is not null
        && MyRegex().IsMatch(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var u)
        && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    [GeneratedRegex(@"<\s*/?\s*(a|i|b|strong|em|code|pre)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex MyRegex();
}
