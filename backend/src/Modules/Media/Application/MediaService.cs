using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Shared.Errors;
using InspectFlow.Shared.Localization;
using InspectFlow.Shared.Security;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Media.Application;

public sealed record MediaDto(Guid Id, string MediaType, Guid? DefectId, string Url, string OriginalFilename,
    string MimeType, long FileSize, DateTimeOffset UploadedAt);

public sealed record UploadMediaCommand(Guid InspectionId, Guid RoomId, MediaType MediaType, Guid? DefectId,
    Stream Content, string FileName, string? DeclaredContentType, long Length);

/// <summary>Builds short-lived read URLs. Only call after the caller has been authorized for the media.</summary>
public sealed class MediaUrlService(IStorageService storage, IOptions<InspectionRulesOptions> rules)
{
    public Task<string> GetUrlAsync(string storageKey, CancellationToken ct) =>
        storage.GetReadUrlAsync(storageKey, TimeSpan.FromMinutes(rules.Value.MediaUrlMinutes), ct);

    public async Task<MediaDto> ToDtoAsync(InspectionRoomMedia m, CancellationToken ct) =>
        new(m.Id, m.MediaType.ToString(), m.DefectId, await GetUrlAsync(m.StorageKey, ct), m.OriginalFilename,
            m.MimeType, m.FileSize, m.UploadedAt);
}

public static class ImageFileValidator
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.Ordinal)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public static IReadOnlyCollection<string> AllowedMimeTypes => Extensions.Keys;

    /// <summary>Detects the real image type from magic bytes. Returns null for anything else.</summary>
    public static string? DetectMimeType(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return "image/jpeg";
        if (header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }

    public static string ExtensionFor(string mimeType) => Extensions[mimeType];

    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? "photo");
        var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or ' ').ToArray()).Trim();
        if (cleaned.Length == 0) cleaned = "photo";
        return cleaned.Length > 120 ? cleaned[^120..] : cleaned;
    }
}

public sealed class MediaService(
    IAppDbContext db,
    InspectionAccess access,
    InspectionWriteScope writeScope,
    IStorageService storage,
    MediaUrlService urls,
    IAuditLogger audit,
    IClock clock,
    IOptions<InspectionRulesOptions> rules,
    ILogger<MediaService> logger)
{
    public Task<MediaDto> UploadAsync(UploadMediaCommand command, CancellationToken ct) =>
        writeScope.RunAsync(command.InspectionId, () => UploadCoreAsync(command, ct), ct);

    private async Task<MediaDto> UploadCoreAsync(UploadMediaCommand command, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(command.InspectionId, ct);
        inspection.EnsureEditableBy(agentId);
        var room = inspection.Rooms.FirstOrDefault(r => r.Id == command.RoomId) ?? throw new NotFoundException(EntityNames.Room, command.RoomId);

        if (command.MediaType == MediaType.Defect)
        {
            if (command.DefectId is null) throw new ValidationException("DefectId", new("Defect photos must reference a defect.", "Fotos de avaria precisam indicar a avaria."));
            room.GetDefect(command.DefectId.Value);
        }
        else if (command.DefectId is not null)
        {
            throw new ValidationException("DefectId", new("General photos cannot reference a defect.", "Fotos gerais não podem indicar uma avaria."));
        }

        var max = rules.Value.MaxUploadBytes;
        if (command.Length <= 0) throw new ValidationException("File", new("The file is empty.", "O arquivo está vazio."));
        if (command.Length > max) throw new ValidationException("File", FileTooLarge(max));

        var existing = await db.InspectionRoomMedia.CountAsync(m => m.InspectionRoomId == room.Id, ct);
        if (existing >= rules.Value.MaxPhotosPerRoom)
            throw new DomainRuleException("media.limit", new($"A room can have at most {rules.Value.MaxPhotosPerRoom} photos.", $"Um cômodo pode ter no máximo {rules.Value.MaxPhotosPerRoom} fotos."));

        // Buffer (bounded by MaxUploadBytes) so we can sniff the real type and hash the content.
        using var buffer = new MemoryStream();
        await command.Content.CopyToAsync(buffer, ct);
        if (buffer.Length > max) throw new ValidationException("File", FileTooLarge(max));
        var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        var detected = ImageFileValidator.DetectMimeType(bytes);
        if (detected is null)
            throw new ValidationException("File", new($"Only JPEG, PNG or WebP images are accepted (got '{command.DeclaredContentType}').", $"Apenas imagens JPEG, PNG ou WebP são aceitas (recebido: '{command.DeclaredContentType}')."));
        if (command.DeclaredContentType is { Length: > 0 } declared &&
            !string.Equals(declared, detected, StringComparison.OrdinalIgnoreCase) &&
            !(declared.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) && detected == "image/jpeg"))
            throw new ValidationException("File", new($"The file content ({detected}) does not match its declared type ({declared}).", $"O conteúdo do arquivo ({detected}) não corresponde ao tipo informado ({declared})."));

        var now = clock.UtcNow;
        var mediaId = Guid.NewGuid();
        var key = StorageKeys.ForRoomMedia(inspection.Id, room.Id, mediaId, ImageFileValidator.ExtensionFor(detected));
        var media = new InspectionRoomMedia
        {
            Id = mediaId,
            InspectionId = inspection.Id,
            InspectionRoomId = room.Id,
            DefectId = command.DefectId,
            MediaType = command.MediaType,
            Status = MediaStatus.Ready,
            StorageKey = key,
            OriginalFilename = ImageFileValidator.SanitizeFileName(command.FileName),
            MimeType = detected,
            FileSize = buffer.Length,
            Sha256 = SecureTokens.Sha256Hex(bytes),
            UploadedBy = agentId,
            UploadedAt = now,
        };

        buffer.Position = 0;
        await storage.SaveAsync(key, buffer, detected, ct);
        try
        {
            db.InspectionRoomMedia.Add(media);
            room.MarkActivity(now);
            audit.Record(AuditActions.PhotoUploaded, nameof(InspectionRoomMedia), media.Id,
                new { inspectionId = inspection.Id, roomId = room.Id, mediaType = media.MediaType.ToString(), media.FileSize, media.MimeType });
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            await TryDeleteAsync(key);
            throw;
        }
        return await urls.ToDtoAsync(media, ct);
    }

    public async Task DeleteAsync(Guid inspectionId, Guid mediaId, CancellationToken ct)
    {
        var key = await writeScope.RunAsync(inspectionId, () => DeleteCoreAsync(inspectionId, mediaId, ct), ct);
        await TryDeleteAsync(key); // only after the database change committed
    }

    private async Task<string> DeleteCoreAsync(Guid inspectionId, Guid mediaId, CancellationToken ct)
    {
        var agentId = access.RequireAgent();
        var inspection = await access.GetForAssignedAgentAsync(inspectionId, ct);
        inspection.EnsureEditableBy(agentId);
        var media = await db.InspectionRoomMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.InspectionId == inspectionId, ct)
                    ?? throw new NotFoundException(EntityNames.Photo, mediaId);
        var room = inspection.Rooms.First(r => r.Id == media.InspectionRoomId);
        if (room.Status == Inspections.Domain.InspectionRoomStatus.Completed) room.Reopen(clock.UtcNow);

        db.InspectionRoomMedia.Remove(media);
        audit.Record(AuditActions.PhotoDeleted, nameof(InspectionRoomMedia), media.Id, new { inspectionId, roomId = media.InspectionRoomId });
        await db.SaveChangesAsync(ct);
        return media.StorageKey;
    }

    public async Task<IReadOnlyList<MediaDto>> ListForRoomAsync(Guid roomId, CancellationToken ct)
    {
        var items = await db.InspectionRoomMedia.AsNoTracking().Where(m => m.InspectionRoomId == roomId)
            .OrderBy(m => m.UploadedAt).ToListAsync(ct);
        var result = new List<MediaDto>(items.Count);
        foreach (var m in items) result.Add(await urls.ToDtoAsync(m, ct));
        return result;
    }

    private async Task TryDeleteAsync(string key)
    {
        try { await storage.DeleteAsync(key); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete storage object {Key}", key); }
    }

    private static LocalizedText FileTooLarge(long maxBytes) => new(
        $"The file exceeds the maximum size of {maxBytes / (1024 * 1024)} MB.",
        $"O arquivo excede o tamanho máximo de {maxBytes / (1024 * 1024)} MB.");
}
