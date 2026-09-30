using InspectFlow.Modules.AI.Domain;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Domain;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Notifications.Domain;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Modules.Reports.Domain;
using InspectFlow.Modules.Tenancies.Domain;
using InspectFlow.Modules.Tenants.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Infrastructure.Persistence;

/// <summary>
/// Single database, one PostgreSQL schema per module. Entity configuration lives in
/// <see cref="ModelConfiguration"/> so domain classes stay free of persistence attributes.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options),
        IAppDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyMember> CompanyMembers => Set<CompanyMember>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyRoom> PropertyRooms => Set<PropertyRoom>();
    public DbSet<Tenancy> Tenancies => Set<Tenancy>();
    public DbSet<TenancyMember> TenancyMembers => Set<TenancyMember>();
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionRoom> InspectionRooms => Set<InspectionRoom>();
    public DbSet<InspectionDefect> InspectionDefects => Set<InspectionDefect>();
    public DbSet<InspectionComparison> InspectionComparisons => Set<InspectionComparison>();
    public DbSet<InspectionInvitation> InspectionInvitations => Set<InspectionInvitation>();
    public DbSet<InspectionRoomMedia> InspectionRoomMedia => Set<InspectionRoomMedia>();
    public DbSet<AiAnalysis> AiAnalyses => Set<AiAnalysis>();
    public DbSet<InspectionReport> InspectionReports => Set<InspectionReport>();
    public DbSet<InspectionReportVersion> InspectionReportVersions => Set<InspectionReportVersion>();
    public DbSet<ReportShareLink> ReportShareLinks => Set<ReportShareLink>();
    public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
    public DbSet<TenantObservation> TenantObservations => Set<TenantObservation>();
    public DbSet<TenantResponse> TenantResponses => Set<TenantResponse>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<BillingEvent> BillingEvents => Set<BillingEvent>();

    DbSet<User> IAppDbContext.Users => Users;
    DbSet<Role> IAppDbContext.Roles => Roles;
    DbSet<UserRole> IAppDbContext.UserRoles => UserRoles;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ModelConfiguration.Apply(builder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var builder = configurationBuilder;
        // Enums are stored as readable strings.
        builder.Properties<InspectionType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<InspectionVisibility>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<InspectionStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<InspectionRoomStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<DefectClassification>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ComparisonDecision>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<ComparisonDecision?>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<DescriptionSource>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<PropertyType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<RoomType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<TenancyStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<CompanyRole>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<MediaType>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<MediaStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<AiAnalysisKind>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<AiAnalysisStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<TenantDecision>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<SubscriptionStatus>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<CheckoutSessionStatus>().HaveConversion<string>().HaveMaxLength(32);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Application-level guard (a database trigger enforces the same rule).</summary>
    private void GuardAppendOnly()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            if (entry.Entity is InspectionReportVersion or AuditLog or TenantResponse or TenantObservation)
                throw new ImmutableRecordException(entry.Entity.GetType().Name);
        }
    }
}

public sealed class ImmutableRecordException(string entity)
    : InvalidOperationException($"{entity} records are append-only and cannot be modified or deleted.");
