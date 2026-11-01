namespace Comments.Application.Common;

/// <summary>
/// Результат операции без исключений: ожидаемые ошибки (неверная капча, невалидное поле, плохой файл) —
/// это обычный исход, а не исключительная ситуация. Дешевле исключений и не мешает отладчику.
/// (Файл можно переименовать в Result.cs.)
/// </summary>
public sealed class Result<T>
{
    private Result(T? value, IReadOnlyDictionary<string, string[]>? errors) { Value = value; Errors = errors; }

    public T? Value { get; }
    /// <summary>Ошибки по полям формы (ключи в camelCase: userName, email, text, captchaAnswer, file, ...).</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
    public bool IsSuccess => Errors is null;

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(IReadOnlyDictionary<string, string[]> errors) => new(default, errors);
    public static Result<T> Fail(string field, string message)
        => Fail(new Dictionary<string, string[]> { [field] = [message] });
}

public sealed record ClientInfo(string IpAddress, string UserAgent);
