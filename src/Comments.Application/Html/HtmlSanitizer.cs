using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Comments.Application.Abstractions;

namespace Comments.Application.Html;

/// <summary>
/// Whitelist-санитайзер. Разрешены только: &lt;a href title&gt;, &lt;code&gt;, &lt;i&gt;, &lt;strong&gt;.
/// Любой другой "тег" превращается в безопасный текст. Разрешённые теги обязаны быть закрыты и правильно
/// вложены, на выходе — валидный XHTML (теги в нижнем регистре, атрибуты в двойных кавычках, переводы строк — &lt;br /&gt;).
/// </summary>
public sealed partial class HtmlSanitizer : IHtmlSanitizer
{
    private const int MaxDepth = 20;

    [GeneratedRegex(@"<[^<>]*>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"^<(/)?(a|code|i|strong)(\s[^<>]*)?>$", RegexOptions.IgnoreCase)]
    private static partial Regex AllowedTag();

    [GeneratedRegex(@"\s+([A-Za-z][A-Za-z-]*)\s*=\s*(?:""([^""]*)""|'([^']*)')")]
    private static partial Regex Attribute();

    public SanitizeResult Sanitize(string text)
    {
        var sb = new StringBuilder(text.Length + 32);
        var stack = new Stack<string>();
        var pos = 0;

        foreach (Match m in AnyTag().Matches(text))
        {
            AppendText(sb, text[pos..m.Index]);
            pos = m.Index + m.Length;

            var tag = AllowedTag().Match(m.Value);
            if (!tag.Success) { AppendText(sb, m.Value); continue; }   // чужой тег -> просто текст

            var closing = tag.Groups[1].Success;
            var name = tag.Groups[2].Value.ToLowerInvariant();
            var attrs = tag.Groups[3].Value;

            if (closing)
            {
                if (!string.IsNullOrWhiteSpace(attrs)) return SanitizeResult.Fail($"Closing tag </{name}> must not have attributes.");
                if (stack.Count == 0 || stack.Peek() != name) return SanitizeResult.Fail($"Unexpected closing tag </{name}>.");
                stack.Pop();
                sb.Append("</").Append(name).Append('>');
                continue;
            }

            if (stack.Count >= MaxDepth) return SanitizeResult.Fail("Tags are nested too deeply.");

            if (name == "a")
            {
                if (stack.Contains("a")) return SanitizeResult.Fail("Nested <a> tags are not allowed.");
                if (!TryParseAnchor(attrs, out var href, out var title, out var error)) return SanitizeResult.Fail(error);
                sb.Append("<a href=\"").Append(WebUtility.HtmlEncode(href)).Append('"');
                if (title is not null) sb.Append(" title=\"").Append(WebUtility.HtmlEncode(title)).Append('"');
                sb.Append(" rel=\"nofollow noopener noreferrer\" target=\"_blank\">");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(attrs)) return SanitizeResult.Fail($"Tag <{name}> must not have attributes.");
                sb.Append('<').Append(name).Append('>');
            }
            stack.Push(name);
        }

        AppendText(sb, text[pos..]);
        if (stack.Count > 0) return SanitizeResult.Fail($"Tag <{stack.Peek()}> is not closed.");
        return SanitizeResult.Ok(sb.ToString());
    }

    private static void AppendText(StringBuilder sb, string s)
    {
        if (s.Length == 0) return;
        sb.Append(WebUtility.HtmlEncode(s).Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "<br />"));
    }

    private static bool TryParseAnchor(string attrs, out string href, out string? title, out string error)
    {
        href = ""; title = null; error = "";
        var seen = new HashSet<string>();

        foreach (Match m in Attribute().Matches(attrs))
        {
            var name = m.Groups[1].Value.ToLowerInvariant();
            var value = m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
            if (!seen.Add(name)) { error = $"Duplicate attribute '{name}'."; return false; }
            switch (name)
            {
                case "href": href = value; break;
                case "title": title = value; break;
                default: error = $"Attribute '{name}' is not allowed on <a>."; return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(Attribute().Replace(attrs, ""))) { error = "Malformed attributes in <a>."; return false; }
        if (!seen.Contains("href")) { error = "<a> requires an href attribute."; return false; }
        if (href.Length > 2048 || !Uri.TryCreate(href, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https" or "mailto"))
        { error = "Link must be an absolute http, https or mailto URL."; return false; }
        if (title is { Length: > 200 }) { error = "Link title is too long."; return false; }
        return true;
    }
}
