namespace InspectFlow.Modules.Common;

/// <summary>Public URLs used to build links sent to users (invitations, shared reports).</summary>
public sealed class AppUrlOptions
{
    public string WebBaseUrl { get; set; } = "http://localhost:3000";

    public string Web(string path) => $"{WebBaseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
}

public sealed class InspectionRulesOptions
{
    public int MinimumGeneralPhotosPerRoom { get; set; } = 1;
    public int MaxPhotosPerRoom { get; set; } = 40;
    public long MaxUploadBytes { get; set; } = 15 * 1024 * 1024;
    public int PrivateInvitationDays { get; set; } = 7;
    public int TenantInvitationDays { get; set; } = 14;
    public int ReportShareLinkDays { get; set; } = 30;
    public int MediaUrlMinutes { get; set; } = 30;
}
