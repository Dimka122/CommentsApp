
namespace CommentsApp.CommentsApp.Application.Interfaces
{
    public interface IHtmlSanitizerService
    {
        string Sanitize(string rawHtml);   // whitelist + проверка парности тегов
    }
}
