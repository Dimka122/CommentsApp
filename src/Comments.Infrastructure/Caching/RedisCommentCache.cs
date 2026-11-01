using System.Text.Json;
using Comments.Application.Abstractions;
using Comments.Application.Comments;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Comments.Infrastructure.Caching;

/// <summary>
/// Кэш страниц списка с версионной инвалидацией: ключ содержит номер версии,
/// при создании комментария версия увеличивается (INCR), старые ключи умирают по TTL.
/// Любая ошибка Redis = промах кэша, чтение из БД продолжает работать.
/// </summary>
public sealed class RedisCommentCache(IConnectionMultiplexer redis, ILogger<RedisCommentCache> log) : ICommentCache
{
    private const string VersionKey = "comments:ver";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<CacheLookup> LookupAsync(CommentSortBy sortBy, bool desc, int page, CancellationToken ct = default)
    {
        try
        {
            var db = redis.GetDatabase();
            var ver = (long?)await db.StringGetAsync(VersionKey) ?? 0;
            var key = $"comments:page:v{ver}:{sortBy}:{(desc ? "desc" : "asc")}:{page}";
            var raw = await db.StringGetAsync(key);
            var value = raw.HasValue ? JsonSerializer.Deserialize<PagedResult<CommentDto>>((string)raw!, Json) : null;
            return new CacheLookup(key, value);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Comment cache lookup failed; falling back to database");
            return new CacheLookup("", null);
        }
    }

    public async Task StoreAsync(string key, PagedResult<CommentDto> value, CancellationToken ct = default)
    {
        if (key.Length == 0) return;
        try { await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value, Json), Ttl); }
        catch (Exception ex) { log.LogWarning(ex, "Comment cache store failed"); }
    }

    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        try { await redis.GetDatabase().StringIncrementAsync(VersionKey); }
        catch (Exception ex) { log.LogWarning(ex, "Comment cache invalidation failed"); }
    }
}
