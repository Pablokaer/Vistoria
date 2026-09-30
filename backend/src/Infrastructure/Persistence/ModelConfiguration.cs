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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InspectFlow.Infrastructure.Persistence;

internal static class ModelConfiguration
{
    public const string IdentitySchema = "identity";

    public static void Apply(ModelBuilder b)
    {
        // ---------------- Identity ----------------
        b.Entity<User>(e =>
        {
            e.ToTable("users", IdentitySchema);
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).IsRequired();
        });
        b.Entity<Role>(e =>
        {
            e.ToTable("roles", IdentitySchema);
            e.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId).IsRequired();
        });
        b.Entity<UserRole>().ToTable("user_roles", IdentitySchema);
        b.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims", IdentitySchema);
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins", IdentitySchema);
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims", IdentitySchema);
        b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens", IdentitySchema);

        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens", IdentitySchema);
            e.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(t => t.CreatedByIp).HasMaxLength(64);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId);
            e.HasIndex(t => t.FamilyId);
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<AgentProfile>(e =>
        {
            e.ToTable("agent_profiles", IdentitySchema);
            e.HasKey(p => p.UserId);
            e.Property(p => p.DisplayName).HasMaxLength(200);
            e.Property(p => p.Phone).HasMaxLength(50);
            e.Property(p => p.ServiceArea).HasMaxLength(200);
            e.HasOne<User>().WithOne().HasForeignKey<AgentProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Companies ----------------
        b.Entity<Company>(e =>
        {
            e.ToTable("companies", "companies");
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.ContactEmail).HasMaxLength(256);
            e.Property(c => c.Phone).HasMaxLength(50);
            e.HasMany(c => c.Members).WithOne().HasForeignKey(m => m.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CompanyMember>(e =>
        {
            e.ToTable("company_members", "companies");
            e.HasIndex(m => new { m.CompanyId, m.UserId }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Properties ----------------
        b.Entity<Property>(e =>
        {
            e.ToTable("properties", "properties");
            e.Property(p => p.AddressLine1).HasMaxLength(200).IsRequired();
            e.Property(p => p.AddressLine2).HasMaxLength(200);
            e.Property(p => p.City).HasMaxLength(100).IsRequired();
            e.Property(p => p.Postcode).HasMaxLength(20).IsRequired();
            e.Property(p => p.Country).HasMaxLength(100).IsRequired();
            e.HasIndex(p => p.CompanyId);
            e.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Rooms).WithOne().HasForeignKey(r => r.PropertyId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(p => p.ActiveRooms);
            e.Ignore(p => p.FullAddress);
        });
        b.Entity<PropertyRoom>(e =>
        {
            e.ToTable("property_rooms", "properties");
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(r => r.PropertyId);
        });

        // ---------------- Tenancies ----------------
        b.Entity<Tenancy>(e =>
        {
            e.ToTable("tenancies", "tenancies");
            e.Property(t => t.Reference).HasMaxLength(100);
            e.HasIndex(t => t.PropertyId);
            e.HasIndex(t => t.CompanyId);
            e.HasOne<Property>().WithMany().HasForeignKey(t => t.PropertyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Company>().WithMany().HasForeignKey(t => t.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(t => t.Members).WithOne().HasForeignKey(m => m.TenancyId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<TenancyMember>(e =>
        {
            e.ToTable("tenancy_members", "tenancies");
            e.Property(m => m.Email).HasMaxLength(256).IsRequired();
            e.Property(m => m.FullName).HasMaxLength(200).IsRequired();
            e.Property(m => m.InvitationTokenHash).HasMaxLength(64);
            e.Property(m => m.Version).IsRowVersion();
            e.HasIndex(m => new { m.TenancyId, m.Email }).IsUnique();
            e.HasIndex(m => m.UserId);
            e.HasIndex(m => m.InvitationTokenHash).IsUnique().HasFilter("invitation_token_hash IS NOT NULL");
            e.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Inspections ----------------
        b.Entity<Inspection>(e =>
        {
            e.ToTable("inspections", "inspections");
            e.Property(i => i.Instructions).HasMaxLength(2000);
            e.Property(i => i.Version).IsRowVersion();
            e.HasIndex(i => new { i.CompanyId, i.Status });
            e.HasIndex(i => new { i.AgentId, i.Status });
            e.HasIndex(i => new { i.Status, i.Visibility });
            e.HasIndex(i => i.PropertyId);
            e.HasIndex(i => i.TenancyId);
            e.HasOne<Company>().WithMany().HasForeignKey(i => i.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Property>().WithMany().HasForeignKey(i => i.PropertyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Tenancy>().WithMany().HasForeignKey(i => i.TenancyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(i => i.AgentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Inspection>().WithMany().HasForeignKey(i => i.ComparisonInspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(i => i.Rooms).WithOne().HasForeignKey(r => r.InspectionId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(i => i.IsFinalized);
        });
        b.Entity<InspectionRoom>(e =>
        {
            e.ToTable("inspection_rooms", "inspections");
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.Property(r => r.AiDescription).HasMaxLength(InspectionRoom.MaxTextLength);
            e.Property(r => r.FinalDescription).HasMaxLength(InspectionRoom.MaxTextLength);
            e.Property(r => r.AgentNotes).HasMaxLength(InspectionRoom.MaxTextLength);
            e.HasIndex(r => r.InspectionId);
            e.HasMany(r => r.Defects).WithOne().HasForeignKey(d => d.InspectionRoomId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<InspectionDefect>(e =>
        {
            e.ToTable("inspection_defects", "inspections");
            e.Property(d => d.Description).HasMaxLength(300);
            e.Property(d => d.Location).HasMaxLength(200);
            e.Property(d => d.AIDescription).HasMaxLength(InspectionRoom.MaxTextLength);
            e.Property(d => d.FinalDescription).HasMaxLength(InspectionRoom.MaxTextLength);
            e.Property(d => d.AIConfidence).HasPrecision(4, 3);
            e.HasIndex(d => d.InspectionRoomId);
        });
        b.Entity<InspectionComparison>(e =>
        {
            e.ToTable("inspection_comparisons", "inspections");
            e.HasIndex(c => c.InspectionRoomId).IsUnique();
            e.HasIndex(c => c.TargetInspectionId);
            e.HasOne<Inspection>().WithMany().HasForeignKey(c => c.SourceInspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Inspection>().WithMany().HasForeignKey(c => c.TargetInspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionRoom>().WithMany().HasForeignKey(c => c.InspectionRoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionRoom>().WithMany().HasForeignKey(c => c.SourceInspectionRoomId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<InspectionInvitation>(e =>
        {
            e.ToTable("inspection_invitations", "inspections");
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.AccessCodeHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.InvitedEmail).HasMaxLength(256);
            e.Property(x => x.Version).IsRowVersion();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.InspectionId);
            e.HasOne<Inspection>().WithMany().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.IsLocked);
        });

        // ---------------- Media ----------------
        b.Entity<InspectionRoomMedia>(e =>
        {
            e.ToTable("inspection_room_media", "media");
            e.Property(m => m.StorageKey).HasMaxLength(300).IsRequired();
            e.Property(m => m.OriginalFilename).HasMaxLength(200).IsRequired();
            e.Property(m => m.MimeType).HasMaxLength(100).IsRequired();
            e.Property(m => m.Sha256).HasMaxLength(64);
            e.Property(m => m.Caption).HasMaxLength(300);
            e.HasIndex(m => m.StorageKey).IsUnique();
            e.HasIndex(m => m.InspectionRoomId);
            e.HasIndex(m => m.InspectionId);
            e.HasOne<Inspection>().WithMany().HasForeignKey(m => m.InspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionRoom>().WithMany().HasForeignKey(m => m.InspectionRoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionDefect>().WithMany().HasForeignKey(m => m.DefectId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- AI ----------------
        b.Entity<AiAnalysis>(e =>
        {
            e.ToTable("ai_analyses", "ai");
            e.Property(a => a.Provider).HasMaxLength(50).IsRequired();
            e.Property(a => a.Model).HasMaxLength(100);
            e.Property(a => a.PromptVersion).HasMaxLength(50).IsRequired();
            e.Property(a => a.ResultJson).HasColumnType("jsonb");
            e.Property(a => a.Error).HasMaxLength(1000);
            e.Property(a => a.Confidence).HasPrecision(4, 3);
            e.HasIndex(a => new { a.InspectionRoomId, a.Kind, a.Status });
            e.HasIndex(a => a.Status);
            e.HasOne<InspectionRoom>().WithMany().HasForeignKey(a => a.InspectionRoomId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(a => a.IsRunning);
        });

        // ---------------- Reports ----------------
        b.Entity<InspectionReport>(e =>
        {
            e.ToTable("inspection_reports", "reports");
            e.Property(r => r.ReportNumber).HasMaxLength(40).IsRequired();
            e.HasIndex(r => r.InspectionId).IsUnique();
            e.HasIndex(r => r.ReportNumber).IsUnique();
            e.HasOne<Inspection>().WithMany().HasForeignKey(r => r.InspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(r => r.Versions).WithOne().HasForeignKey(v => v.ReportId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<InspectionReportVersion>(e =>
        {
            e.ToTable("inspection_report_versions", "reports");
            // text (not jsonb) preserves the exact bytes the SHA-256 was computed on.
            e.Property(v => v.SnapshotJson).HasColumnType("text").IsRequired();
            e.Property(v => v.SnapshotSha256).HasMaxLength(64).IsRequired();
            e.Property(v => v.PdfStorageKey).HasMaxLength(300);
            e.Property(v => v.PdfSha256).HasMaxLength(64);
            e.Property(v => v.Reason).HasMaxLength(300);
            e.HasIndex(v => new { v.ReportId, v.VersionNumber }).IsUnique();
        });
        b.Entity<ReportShareLink>(e =>
        {
            e.ToTable("report_share_links", "reports");
            e.Property(l => l.TokenHash).HasMaxLength(64).IsRequired();
            e.HasIndex(l => l.TokenHash).IsUnique();
            e.HasIndex(l => l.ReportId);
            e.HasOne<InspectionReport>().WithMany().HasForeignKey(l => l.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------- Tenants ----------------
        b.Entity<TenantProfile>(e =>
        {
            e.ToTable("tenant_profiles", "tenants");
            e.HasKey(p => p.UserId);
            e.Property(p => p.Phone).HasMaxLength(50);
            e.HasOne<User>().WithOne().HasForeignKey<TenantProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<TenantObservation>(e =>
        {
            e.ToTable("tenant_observations", "tenants");
            e.Property(o => o.Text).HasMaxLength(4000).IsRequired();
            e.HasIndex(o => o.InspectionId);
            e.HasOne<Inspection>().WithMany().HasForeignKey(o => o.InspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionReportVersion>().WithMany().HasForeignKey(o => o.ReportVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionRoom>().WithMany().HasForeignKey(o => o.InspectionRoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(o => o.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<TenantResponse>(e =>
        {
            e.ToTable("tenant_responses", "tenants");
            e.Property(r => r.Comment).HasMaxLength(4000);
            e.HasIndex(r => r.InspectionId);
            e.HasOne<Inspection>().WithMany().HasForeignKey(r => r.InspectionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InspectionReportVersion>().WithMany().HasForeignKey(r => r.ReportVersionId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------- Notifications & Audit ----------------
        b.Entity<Notification>(e =>
        {
            e.ToTable("notifications", "notifications");
            e.Property(n => n.Type).HasMaxLength(100).IsRequired();
            e.Property(n => n.Subject).HasMaxLength(300).IsRequired();
            e.Property(n => n.Body).HasMaxLength(4000).IsRequired();
            e.Property(n => n.Link).HasMaxLength(1000);
            e.Property(n => n.RecipientEmail).HasMaxLength(256);
            e.HasIndex(n => new { n.RecipientUserId, n.CreatedAt });
        });
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs", "audit");
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            e.Property(a => a.Metadata).HasColumnType("jsonb");
            e.Property(a => a.IpAddress).HasMaxLength(64);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.HasIndex(a => a.Timestamp);
            e.HasIndex(a => a.UserId);
        });

        // Module entities receive their Guid ids in the domain (Guid.NewGuid()). Marking keys as never
        // generated makes EF treat new children discovered through navigations as Added (not Modified).
        foreach (var entity in b.Model.GetEntityTypes())
        {
            if (entity.ClrType.Namespace?.StartsWith("InspectFlow.Modules", StringComparison.Ordinal) != true) continue;
            if (entity.ClrType == typeof(User) || entity.ClrType == typeof(Role) || entity.ClrType == typeof(UserRole)) continue;
            var key = entity.FindPrimaryKey();
            if (key is { Properties.Count: 1 } && key.Properties[0].ClrType == typeof(Guid))
                key.Properties[0].ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
        }
    }
}
