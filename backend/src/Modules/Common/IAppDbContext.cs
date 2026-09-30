using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Notifications.Domain;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Modules.Reports.Domain;
using InspectFlow.Modules.Tenancies.Domain;
using InspectFlow.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace InspectFlow.Modules.Common;

/// <summary>
/// Unit of work used by the application services. One PostgreSQL database, one schema per module.
/// Implemented by Infrastructure's AppDbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AgentProfile> AgentProfiles { get; }

    DbSet<Company> Companies { get; }
    DbSet<CompanyMember> CompanyMembers { get; }

    DbSet<Property> Properties { get; }
    DbSet<PropertyRoom> PropertyRooms { get; }

    DbSet<Tenancy> Tenancies { get; }
    DbSet<TenancyMember> TenancyMembers { get; }

    DbSet<Inspection> Inspections { get; }
    DbSet<InspectionRoom> InspectionRooms { get; }
    DbSet<InspectionDefect> InspectionDefects { get; }
    DbSet<InspectionComparison> InspectionComparisons { get; }
    DbSet<InspectionInvitation> InspectionInvitations { get; }

    DbSet<InspectionRoomMedia> InspectionRoomMedia { get; }
    DbSet<AiAnalysis> AiAnalyses { get; }

    DbSet<InspectionReport> InspectionReports { get; }
    DbSet<InspectionReportVersion> InspectionReportVersions { get; }
    DbSet<ReportShareLink> ReportShareLinks { get; }

    DbSet<TenantProfile> TenantProfiles { get; }
    DbSet<TenantObservation> TenantObservations { get; }
    DbSet<TenantResponse> TenantResponses { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
