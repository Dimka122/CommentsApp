using FluentValidation;
using global::CommentsApp.CommentsApp.Application.Dtos;

namespace CommentsApp.CommentsApp.Application.Validation;

public class CommentCreateValidator : AbstractValidator<CommentCreateDto>
{
    private static readonly string[] AllowedTags = ["a", "i", "strong", "b", "code"];

    public CommentCreateValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Имя обязательно")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().WithMessage("Некорректный email");

        RuleFor(x => x.Homepage)
            .Must(uri => string.IsNullOrWhiteSpace(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Homepage должен быть корректным URL");

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(5000);

        RuleFor(x => x.CaptchaToken).NotEmpty();
        RuleFor(x => x.CaptchaCode).NotEmpty();

        RuleFor(x => x.File)
            .Must(f => f is null
                || (f.Length <= 100 * 1024 && IsAllowedType(f.ContentType)))
            .WithMessage("Файл: max 100 КБ, разрешены txt и изображения (jpeg/png/gif)");
    }

    private static bool IsAllowedType(string contentType) => contentType switch
    {
        "text/plain" or "image/jpeg" or "image/png" or "image/gif" => true,
        _ => false
    };
}

