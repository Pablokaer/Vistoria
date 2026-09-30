using InspectFlow.Modules.Media.Application;
using InspectFlow.Shared.Time;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure.Storage;

/// <summary>
/// Development storage on local disk. Objects are write-once (existing keys are never overwritten),
/// served through signed, expiring URLs handled by the API's /api/files endpoint.
/// </summary>
public sealed class LocalFileStorageService : IStorageService
{
    private readonly string _root;
    private readonly UrlSigner _signer;
    private readonly IClock _clock;
    private readonly StorageOptions _options;

    public LocalFileStorageService(IOptions<StorageOptions> options, UrlSigner signer, IClock clock, IHostEnvironment env)
    {
        _options = options.Value;
        _signer = signer;
        _clock = clock;
        var root = _options.Local.RootPath;
        _root = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(env.ContentRootPath, root));
        Directory.CreateDirectory(_root);
    }

    public string ProviderName => "local";

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".uploading-" + Guid.NewGuid().ToString("N");
        await using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await content.CopyToAsync(file, cancellationToken);
        File.Move(temp, path, overwrite: false);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (!File.Exists(path)) throw new FileNotFoundException("Storage object not found.", key);
        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(File.Exists(Resolve(key)));

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<string> GetReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        Resolve(key);
        var expires = _clock.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/api/files/{key}?exp={expires}&sig={_signer.Sign(key, expires)}";
        return Task.FromResult(url);
    }

    public Task<DirectUploadTicket?> CreateDirectUploadAsync(string key, string contentType, long maxBytes, TimeSpan lifetime,
        CancellationToken cancellationToken = default) => Task.FromResult<DirectUploadTicket?>(null);

    private string Resolve(string key)
    {
        if (!StorageKeys.IsValid(key)) throw new ArgumentException("Invalid storage key.", nameof(key));
        var full = Path.GetFullPath(Path.Combine(_root, key));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Invalid storage key.", nameof(key));
        return full;
    }
}
