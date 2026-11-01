namespace Comments.Application.Common;

public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors) : base("Validation failed") => Errors = errors;

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] }) { }
}

public sealed record ClientInfo(string IpAddress, string UserAgent);
