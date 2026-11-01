using System.Text;
using Comments.Application.Abstractions;
using Comments.Application.Common;
using Comments.Domain;
using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comments.Application.Comments;

public sealed class CommentService(
    IAppDbContext db, IHtmlSanitizer sanitizer, ICaptchaService captcha,
    IImageProcessor images, IFileStorage storage, ICommentEventPublisher events, ICommentCache cache)
{
    public const int PageSize = 25;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private sealed record PreparedFile(AttachmentKind Kind, byte[] Bytes, string Extension);

    // ---------- Preview ----------
    public string Preview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ValidationException("text", "Text is required.");
        if (text.Length > CommentValidator.MaxTextLength) throw new ValidationException("text", "Text is too long.");
        var r = sanitizer.Sanitize(text);
        if (!r.IsValid) throw new ValidationException("text", r.Error!);
        return r.Html;
    }

    // ---------- Create ----------
    public async Task<CommentDto> CreateAsync(CreateCommentCommand cmd, ClientInfo client, CancellationToken ct = default)
    {
        // 1. Дешёвая валидация (до капчи и тяжёлой обработки файлов).
        var errors = CommentValidator.Validate(cmd);
        var sanitized = string.IsNullOrWhiteSpace(cmd.Text) ? null : sanitizer.Sanitize(cmd.Text);
        if (sanitized is { IsValid: false }) errors.TryAdd("text", [sanitized.Error!]);
        if (errors.Count > 0) throw CommentValidator.ToException(errors);

        // 2. CAPTCHA (одноразовая).
        if (!await captcha.ValidateAsync(cmd.CaptchaId!, cmd.CaptchaAnswer!, ct))
            throw new ValidationException("captchaAnswer", "CAPTCHA is incorrect or expired.");

        // 3. Родитель.
        if (cmd.ParentId is { } parentId && !await db.Comments.AnyAsync(c => c.Id == parentId, ct))
            throw new ValidationException("parentId", "Parent comment not found.");

        // 4. Файл (ресайз/проверка содержимого).
        var file = cmd.File is null ? null : await PrepareFileAsync(cmd.File, ct);

        string? storedPath = null;
        try
        {
            var email = cmd.Email!.Trim().ToLowerInvariant();
            var userName = cmd.UserName!.Trim();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.UserName == userName, ct)
                       ?? User.Create(userName, email, cmd.Homepage, client.IpAddress, client.UserAgent);

            var comment = Comment.Create(user, cmd.ParentId, cmd.Text!, sanitized!.Html);

            if (file is not null)
            {
                storedPath = await storage.SaveAsync(file.Bytes, file.Extension, ct);
                var original = SafeFileName(cmd.File!.FileName);
                comment.AddAttachment(Attachment.Create(comment.Id, file.Kind, storedPath, original, file.Bytes.LongLength));
            }

            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);
            await cache.InvalidateAsync(ct);
            await events.PublishAsync(new CommentCreated(comment.Id, comment.ParentId, user.Id, comment.CreatedAt), ct);
            return (await GetByIdAsync(comment.Id, ct))!;
        }
        catch
        {
            if (storedPath is not null) await storage.DeleteAsync(storedPath, CancellationToken.None);
            throw;
        }
    }

    private async Task<PreparedFile> PrepareFileAsync(UploadedFile f, CancellationToken ct)
    {
        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();

        await using var s = f.OpenRead();
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        if (ext == ".txt")
        {
            if (bytes.Length > 100 * 1024) throw new ValidationException("file", "Text file must not exceed 100 KB.");
            string content;
            try { content = StrictUtf8.GetString(bytes); }
            catch (DecoderFallbackException) { throw new ValidationException("file", "Text file must be valid UTF-8."); }
            if (content.Any(ch => char.IsControl(ch) && ch is not ('\n' or '\r' or '\t')))
                throw new ValidationException("file", "Text file contains unsupported characters.");
            return new PreparedFile(AttachmentKind.Text, bytes, ".txt");
        }

        var img = await images.ProcessAsync(bytes, ct)
                  ?? throw new ValidationException("file", "File is not a valid JPG, GIF or PNG image.");
        return new PreparedFile(AttachmentKind.Image, img.Bytes, img.Extension);
    }

    private static string SafeFileName(string name)
    {
        var n = Path.GetFileName(name);
        n = new string(n.Where(c => !char.IsControl(c)).ToArray());
        return n.Length > 200 ? n[^200..] : (n.Length == 0 ? "file" : n);
    }

    // ---------- Read ----------
    private static IQueryable<CommentDto> Project(IQueryable<Comment> q) =>
        q.Select(c => new CommentDto(
            c.Id, c.ParentId, c.User.UserName, c.User.Email, c.User.Homepage, c.TextHtml, c.CreatedAt,
            c.Attachments.Select(a => new AttachmentDto(a.Id, a.Kind, a.OriginalName, a.SizeBytes)).ToList()));

    public async Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Project(db.Comments.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct);

    /// <summary>Корневые комментарии (с сортировкой и пагинацией по 25) вместе с деревом ответов.</summary>
    public async Task<PagedResult<CommentDto>> GetPageAsync(CommentSortBy sortBy, bool desc, int page, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var lookup = await cache.LookupAsync(sortBy, desc, page, ct);
        if (lookup.Value is not null) return lookup.Value;

        var result = await LoadPageAsync(sortBy, desc, page, ct);
        if (result.Items.Count > 0) await cache.StoreAsync(lookup.Key, result, ct); // пустые/несуществующие страницы не кэшируем
        return result;
    }

    private async Task<PagedResult<CommentDto>> LoadPageAsync(CommentSortBy sortBy, bool desc, int page, CancellationToken ct)
    {
        var rootsQuery = db.Comments.AsNoTracking().Where(c => c.ParentId == null);
        var total = await rootsQuery.CountAsync(ct);

        var ordered = sortBy switch
        {
            CommentSortBy.UserName => Order(rootsQuery, c => c.User.UserName, desc),
            CommentSortBy.Email => Order(rootsQuery, c => c.User.Email, desc),
            _ => Order(rootsQuery, c => c.CreatedAt, desc),
        };

        var roots = await Project(ordered.Skip((page - 1) * PageSize).Take(PageSize)).ToListAsync(ct);

        // Ответы подгружаем уровень за уровнем (позже эту часть заменит GraphQL + кэш).
        var all = new List<CommentDto>(roots);
        var level = roots.Select(r => r.Id).ToList();
        for (var depth = 0; depth < 50 && level.Count > 0; depth++)
        {
            var parents = level;
            var children = await Project(db.Comments.AsNoTracking()
                .Where(c => c.ParentId != null && parents.Contains(c.ParentId.Value))
                .OrderBy(c => c.CreatedAt)).ToListAsync(ct);
            all.AddRange(children);
            level = children.Select(c => c.Id).ToList();
        }

        var map = all.ToDictionary(c => c.Id);
        foreach (var c in all.Where(c => c.ParentId.HasValue)) map[c.ParentId!.Value].Replies.Add(c);

        return new PagedResult<CommentDto>(roots, page, PageSize, total);
    }

    private static IOrderedQueryable<Comment> Order<TKey>(
        IQueryable<Comment> q, System.Linq.Expressions.Expression<Func<Comment, TKey>> key, bool desc)
        => desc ? q.OrderByDescending(key).ThenByDescending(c => c.Id) : q.OrderBy(key).ThenBy(c => c.Id);

    // ---------- Attachments ----------
    public async Task<AttachmentFile?> OpenAttachmentAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;
        var type = Path.GetExtension(a.StoragePath).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream"
        };
        try { return new AttachmentFile(await storage.OpenReadAsync(a.StoragePath, ct), type); }
        catch (FileNotFoundException) { return null; }
    }
}
