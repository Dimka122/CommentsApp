namespace CommentsApp.CommentsApp.Application.Exceptions; 

public class InvalidCaptchaException : Exception
{
    public InvalidCaptchaException()
        : base("CAPTCHA verification failed.") { }

    public InvalidCaptchaException(string message)
        : base(message) { }
}

