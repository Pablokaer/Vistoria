namespace InspectFlow.Modules.Media.Domain;

public enum MediaType
{
    General,
    Defect,
}

public enum MediaStatus
{
    /// <summary>Reserved for direct-to-storage uploads (signed URL) awaiting confirmation.</summary>
    Pending,
    Ready,
}

/// <summary>Metadata of a photo. The binary lives in object storage under <see cref="StorageKey"/>, never in PostgreSQL.</summary>
public class InspectionRoomMedia
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public Guid InspectionRoomId { get; set; }
    public Guid? DefectId { get; set; }
    public MediaType MediaType { get; set; }
    public MediaStatus Status { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Sha256 { get; set; }
    public string? Caption { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
