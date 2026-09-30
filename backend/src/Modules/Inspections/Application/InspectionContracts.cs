using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Modules.Tenancies.Application;

namespace InspectFlow.Modules.Inspections.Application;

public sealed record CreateInspectionRequest(
    Guid PropertyId,
    Guid? TenancyId,
    InspectionType InspectionType,
    InspectionVisibility Visibility,
    Guid? ComparisonInspectionId,
    string? Instructions,
    DateOnly? ScheduledDate,
    DateTimeOffset? AcceptBy,
    bool PublishNow,
    string? InviteEmail);

public sealed record UpdateDraftRequest(InspectionVisibility Visibility, string? Instructions, DateOnly? ScheduledDate, DateTimeOffset? AcceptBy);

public sealed record RegenerateInvitationRequest(string? InviteEmail);

/// <summary>Plain-text token and code are returned exactly once; only hashes are stored.</summary>
public sealed record PrivateInvitationDto(string Link, string AccessCode, DateTimeOffset ExpiresAt, int MaxAttempts);

public sealed record InvitationStatusDto(DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int AttemptCount, int MaxAttempts,
    bool Used, DateTimeOffset? UsedAt, bool Revoked, bool Expired, string? InvitedEmail);

public sealed record PublishResultDto(InspectionDetailsDto Inspection, PrivateInvitationDto? Invitation);

public sealed record InspectionSummaryDto(
    Guid Id,
    Guid PropertyId,
    string PropertyAddress,
    Guid? TenancyId,
    string InspectionType,
    string Visibility,
    string Status,
    string? AgentName,
    int RoomsCompleted,
    int RoomsTotal,
    DateOnly? ScheduledDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record PersonDto(Guid Id, string FullName, string? Email, string? Phone);

public sealed record RoomProgressDto(Guid Id, string Name, string RoomType, int Sequence, string Status,
    int GeneralPhotoCount, int DefectCount, bool HasAiDescription, bool HasFinalDescription, bool? ComparisonDecided);

public sealed record ReportRefDto(Guid ReportId, string ReportNumber, int LatestVersion, DateTimeOffset GeneratedAt);

public sealed record ComparisonRefDto(Guid InspectionId, string InspectionType, DateTimeOffset? CompletedAt, string? ReportNumber);

public sealed record PropertyRefDto(Guid Id, string AddressLine1, string? AddressLine2, string City, string Postcode, string Country, string PropertyType);

public sealed record InspectionDetailsDto(
    Guid Id,
    string InspectionType,
    string Visibility,
    string Status,
    PropertyRefDto Property,
    TenancyDto? Tenancy,
    PersonDto? Agent,
    string CompanyName,
    ComparisonRefDto? ComparisonInspection,
    string? Instructions,
    DateOnly? ScheduledDate,
    DateTimeOffset? AcceptBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? SentToTenantAt,
    DateTimeOffset? TenantRespondedAt,
    IReadOnlyList<RoomProgressDto> Rooms,
    int RoomsCompleted,
    ReportRefDto? Report,
    InvitationStatusDto? Invitation,
    IReadOnlyList<string> AllowedActions);

/// <summary>What an agent sees before accepting: no street address, no tenant data.</summary>
public sealed record AvailableInspectionDto(
    Guid Id,
    string InspectionType,
    string Visibility,
    string CompanyName,
    string City,
    string PostcodeArea,
    string PropertyType,
    int RoomCount,
    IReadOnlyList<string> RoomNames,
    DateOnly? ScheduledDate,
    DateTimeOffset? AcceptBy,
    DateTimeOffset? PublishedAt,
    bool HasComparison);

public sealed record AgentDashboardDto(int Available, int Assigned, int InProgress, int InReview, int Completed,
    IReadOnlyList<InspectionSummaryDto> Active);

public sealed record DefectDto(
    Guid Id,
    string? Description,
    string? Location,
    string Classification,
    string? AIDescription,
    decimal? AIConfidence,
    string? FinalDescription,
    bool AgentConfirmed,
    IReadOnlyList<MediaDto> Photos,
    AiAnalysisDto? LatestAnalysis);

public sealed record BaselineDefectDto(string? Title, string? Location, string Classification, string? Description, IReadOnlyList<string> PhotoUrls);

public sealed record BaselineRoomDto(string Name, string? Description, string? AgentNotes, bool DefectsFound,
    IReadOnlyList<string> PhotoUrls, IReadOnlyList<BaselineDefectDto> Defects, string? ReportNumber, DateTimeOffset? CompletedAt);

public sealed record ComparisonDto(
    Guid Id,
    string? BasicComparison,
    string? AIAnalysis,
    string? AgentDecision,
    string? AgentNotes,
    DateTimeOffset? DecidedAt,
    AiAnalysisDto? LatestAnalysis,
    BaselineRoomDto? Baseline);

public sealed record RoomDetailDto(
    Guid Id,
    Guid InspectionId,
    string Name,
    string RoomType,
    int Sequence,
    string Status,
    bool IsRequired,
    string? AiDescription,
    DateTimeOffset? AiDescriptionGeneratedAt,
    string? FinalDescription,
    string FinalDescriptionSource,
    bool DefectsFound,
    string? AgentNotes,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<MediaDto> GeneralPhotos,
    IReadOnlyList<DefectDto> Defects,
    AiAnalysisDto? LatestAnalysis,
    ComparisonDto? Comparison,
    IReadOnlyList<string> CompletionIssues,
    bool Editable,
    string InspectionStatus,
    Guid? PreviousRoomId,
    Guid? NextRoomId,
    int RoomsCompleted,
    int RoomsTotal);

public sealed record UpdateRoomRequest(string? FinalDescription, bool DefectsFound, string? AgentNotes);

public sealed record AddDefectRequest(string? Description, string? Location);

public sealed record UpdateDefectRequest(string? Description, string? Location, string? FinalDescription,
    DefectClassification Classification, bool AgentConfirmed);

public sealed record ComparisonDecisionRequest(ComparisonDecision Decision, string? Notes);

public sealed record VerifyInvitationRequest(string AccessCode);

public sealed record InvitationPreviewDto(bool RequiresCode, DateTimeOffset ExpiresAt, int AttemptsRemaining, AvailableInspectionDto? Inspection);

public sealed record ReviewDto(InspectionDetailsDto Inspection, ReportSnapshot Preview, IReadOnlyDictionary<Guid, string> PhotoUrls,
    IReadOnlyList<string> BlockingIssues, bool CanFinalize);
