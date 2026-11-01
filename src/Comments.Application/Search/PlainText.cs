using System.Net;
using System.Text.RegularExpressions;

namespace Comments.Application.Search;

public static partial class PlainText
{
    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)] private static partial Regex Br();
    [GeneratedRegex(@"<[^>]*>")] private static partial Regex Tags();
    [GeneratedRegex(@"\s+")] private static partial Regex Whitespace();

    /// <summary>Санитизированный HTML комментария -> обычный текст для индексации.</summary>
    public static string FromHtml(string html)
    {
        var s = Br().Replace(html, " ");
        s = Tags().Replace(s, "");
        s = WebUtility.HtmlDecode(s);
        return Whitespace().Replace(s, " ").Trim();
    }
}
