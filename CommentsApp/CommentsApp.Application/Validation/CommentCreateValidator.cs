using FluentValidation;
using global::CommentsApp.CommentsApp.Application.Dtos;

namespace CommentsApp.CommentsApp.Application.Validation;

public class CommentCreateValidator : AbstractValidator<CommentCreateDto>
{
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

        // Правило для File удалено — файл валидируется в FileStorage.SaveAsync
        // (FileTooLargeException / UnsupportedFileTypeException → 400 в контроллере)
    }
}
