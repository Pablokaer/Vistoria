namespace InspectFlow.Modules.Media.Application;

/// <summary>
/// Object storage abstraction. Implementations: local disk (development) today; Azure Blob / S3 later.
/// Keys are opaque, unique and never contain user-provided data.
/// </summary>
public interface IStorageService
{
    string ProviderName { get; }

    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Short-lived URL for reading (signed local URL today, presigned URL for S3/Blob later).</summary>
    Task<string> GetReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Direct browser/mobile → storage upload. Returns null when the provider does not support it,
    /// in which case clients upload through the API.
    /// </summary>
    Task<DirectUploadTicket?> CreateDirectUploadAsync(string key, string contentType, long maxBytes, TimeSpan lifetime,
        CancellationToken cancellationToken = default);
}

public sealed record DirectUploadTicket(string Url, string Method, IReadOnlyDictionary<string, string> Headers, DateTimeOffset ExpiresAt);

public static class StorageKeys
{
    public static string ForRoomMedia(Guid inspectionId, Guid roomId, Guid mediaId, string extension) =>
        $"inspections/{inspectionId:N}/rooms/{roomId:N}/{mediaId:N}{extension}";

    public static string ForReportPdf(Guid reportId, int version) =>
        $"reports/{reportId:N}/v{version}/{Guid.NewGuid():N}.pdf";

    /// <summary>Rejects path traversal and unexpected characters before touching storage.</summary>
    public static bool IsValid(string key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length <= 300 && !key.Contains("..", StringComparison.Ordinal) &&
        !key.StartsWith('/') && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '/' or '.' or '-' or '_');
}
