namespace InspectFlow.Modules.Audit.Domain;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>JSON. Must never contain secrets (tokens, codes, passwords, API keys).</summary>
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
}

public static class AuditActions
{
    public const string UserRegistered = nameof(UserRegistered);
    public const string UserLoggedIn = nameof(UserLoggedIn);
    public const string CompanyCreated = nameof(CompanyCreated);
    public const string CompanyReportLanguageChanged = nameof(CompanyReportLanguageChanged);
    public const string PropertyCreated = nameof(PropertyCreated);
    public const string PropertyUpdated = nameof(PropertyUpdated);
    public const string TenancyCreated = nameof(TenancyCreated);
    public const string TenantInvited = nameof(TenantInvited);
    public const string TenantJoinedTenancy = nameof(TenantJoinedTenancy);
    public const string InspectionCreated = nameof(InspectionCreated);
    public const string InspectionPublished = nameof(InspectionPublished);
    public const string InspectionInvitationCreated = nameof(InspectionInvitationCreated);
    public const string InspectionInvitationFailedAttempt = nameof(InspectionInvitationFailedAttempt);
    public const string InspectionInvitationVerified = nameof(InspectionInvitationVerified);
    public const string InspectionAccepted = nameof(InspectionAccepted);
    public const string InspectionStarted = nameof(InspectionStarted);
    public const string InspectionCancelled = nameof(InspectionCancelled);
    public const string InspectionExpired = nameof(InspectionExpired);
    public const string InspectionSubmittedForReview = nameof(InspectionSubmittedForReview);
    public const string PhotoUploaded = nameof(PhotoUploaded);
    public const string PhotoDeleted = nameof(PhotoDeleted);
    public const string AIAnalysisRequested = nameof(AIAnalysisRequested);
    public const string AIAnalysisCompleted = nameof(AIAnalysisCompleted);
    public const string AIAnalysisFailed = nameof(AIAnalysisFailed);
    public const string DescriptionEdited = nameof(DescriptionEdited);
    public const string DefectRecorded = nameof(DefectRecorded);
    public const string DefectUpdated = nameof(DefectUpdated);
    public const string DefectRemoved = nameof(DefectRemoved);
    public const string ComparisonDecisionRecorded = nameof(ComparisonDecisionRecorded);
    public const string RoomCompleted = nameof(RoomCompleted);
    public const string RoomReopened = nameof(RoomReopened);
    public const string InspectionFinalized = nameof(InspectionFinalized);
    public const string ReportGenerated = nameof(ReportGenerated);
    public const string ReportShared = nameof(ReportShared);
    public const string InspectionSentToTenant = nameof(InspectionSentToTenant);
    public const string TenantViewed = nameof(TenantViewed);
    public const string TenantCommented = nameof(TenantCommented);
    public const string TenantAccepted = nameof(TenantAccepted);
    public const string TenantDisputed = nameof(TenantDisputed);
    public const string SubscriptionCheckoutStarted = nameof(SubscriptionCheckoutStarted);
    public const string SubscriptionActivated = nameof(SubscriptionActivated);
    public const string SubscriptionPastDue = nameof(SubscriptionPastDue);
    public const string SubscriptionCancelled = nameof(SubscriptionCancelled);
    public const string SubscriptionExpired = nameof(SubscriptionExpired);
}
