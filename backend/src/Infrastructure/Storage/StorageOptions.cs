namespace InspectFlow.Infrastructure.Storage;

public sealed class StorageOptions
{
    /// <summary>"Local" (default). Future: "S3", "AzureBlob".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>HMAC key for signed read URLs. Supplied via STORAGE_SIGNING_KEY.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Prefix for generated URLs. Empty = relative ("/api/files/..."), which works behind the web proxy.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    public LocalStorageOptions Local { get; set; } = new();
}

public sealed class LocalStorageOptions
{
    public string RootPath { get; set; } = "data/storage";
}
