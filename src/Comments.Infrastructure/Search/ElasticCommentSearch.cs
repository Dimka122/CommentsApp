using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Comments.Application.Abstractions;

namespace Comments.Infrastructure.Search;

/// <summary>Elasticsearch через REST (HttpClient): без лишних зависимостей и привязки к версии клиента.</summary>
public sealed class ElasticCommentSearch(HttpClient http) : ICommentSearch
{
    private const string Index = "comments";
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static volatile bool _indexReady;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const string Mapping = """
    {
      "settings": { "number_of_shards": 1, "number_of_replicas": 0 },
      "mappings": { "properties": {
        "id":        { "type": "keyword" },
        "parentId":  { "type": "keyword" },
        "userName":  { "type": "text", "fields": { "raw": { "type": "keyword" } } },
        "email":     { "type": "keyword" },
        "text":      { "type": "text" },
        "createdAt": { "type": "date" }
      } }
    }
    """;

    public async Task IndexAsync(CommentSearchDoc doc, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);
        // PUT по id идемпотентен: повторная доставка сообщения не создаёт дублей.
        using var res = await http.PutAsJsonAsync($"{Index}/_doc/{doc.Id}", doc, Json, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<SearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);
        var body = new
        {
            from = (page - 1) * pageSize,
            size = pageSize,
            track_total_hits = true,
            // multi_match, а не query_string: пользовательский ввод не может подмешать синтаксис запроса.
            query = new { multi_match = new { query, fields = new[] { "text^2", "userName" }, @operator = "and" } },
            highlight = new
            {
                encoder = "html", // экранирует содержимое, оставляя только наши теги подсветки
                pre_tags = new[] { "<mark>" },
                post_tags = new[] { "</mark>" },
                fields = new { text = new { fragment_size = 160, number_of_fragments = 1 } }
            },
            sort = new object[] { "_score", new { createdAt = "desc" } }
        };

        using var res = await http.PostAsJsonAsync($"{Index}/_search", body, Json, ct);
        res.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var hits = json.RootElement.GetProperty("hits");
        var total = hits.GetProperty("total").GetProperty("value").GetInt32();
        var items = new List<SearchHit>();
        foreach (var h in hits.GetProperty("hits").EnumerateArray())
        {
            var src = h.GetProperty("_source");
            string snippet;
            if (h.TryGetProperty("highlight", out var hl) && hl.TryGetProperty("text", out var frag) && frag.GetArrayLength() > 0)
                snippet = frag[0].GetString()!;
            else
            {
                var t = src.GetProperty("text").GetString() ?? "";
                snippet = WebUtility.HtmlEncode(t.Length > 160 ? t[..160] + "…" : t);
            }
            Guid? parent = src.TryGetProperty("parentId", out var p) && p.ValueKind == JsonValueKind.String ? p.GetGuid() : null;
            items.Add(new SearchHit(src.GetProperty("id").GetGuid(), parent,
                src.GetProperty("userName").GetString()!, snippet, src.GetProperty("createdAt").GetDateTime()));
        }
        return new SearchPage(items, page, pageSize, total);
    }

    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_indexReady) return;
        await Gate.WaitAsync(ct);
        try
        {
            if (_indexReady) return;
            using var res = await http.PutAsync(Index, new StringContent(Mapping, Encoding.UTF8, "application/json"), ct);
            if (!res.IsSuccessStatusCode)
            {
                var text = await res.Content.ReadAsStringAsync(ct);
                if (!text.Contains("resource_already_exists_exception"))
                    throw new HttpRequestException($"Elasticsearch index creation failed: {(int)res.StatusCode} {text}");
            }
            _indexReady = true;
        }
        finally { Gate.Release(); }
    }
}
