using System.Text.RegularExpressions;
using Comments.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Comments.Infrastructure.Storage;

public sealed class StorageOptions { public string Root { get; set; } = "uploads"; }

public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.Root);
        Directory.CreateDirectory(_root);
    }

    [GeneratedRegex(@"^\.[a-z0-9]{1,5}$")] private static partial Regex ExtRx();

    public async Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default)
    {
        if (!ExtRx().IsMatch(extension)) throw new ArgumentException("Invalid extension", nameof(extension));
        var now = DateTime.UtcNow;
        var rel = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var full = Resolve(rel);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content, ct);
        return rel;
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
        => Task.FromResult<Stream>(new FileStream(Resolve(path), FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true));

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var full = Resolve(path);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    private string Resolve(string rel)
    {
        var full = Path.GetFullPath(Path.Combine(_root, rel));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Path escapes storage root.");
        return full;
    }
}
