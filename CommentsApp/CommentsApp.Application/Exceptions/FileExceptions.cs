namespace CommentsApp.CommentsApp.Application.Exceptions;

public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
}

public class FileTooLargeException : FileValidationException
{
    public FileTooLargeException(string ext)
        : base($"Файл {ext} превышает допустимый размер.") { }
}

public class UnsupportedFileTypeException : FileValidationException
{
    public UnsupportedFileTypeException(string ext)
        : base($"Тип файла {ext} не поддерживается. Разрешены: .jpg, .jpeg, .png, .gif, .txt") { }
}
