using InspectFlow.Infrastructure.Identity;
using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Media.Domain;
using InspectFlow.Modules.Properties.Application;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Modules.Tenancies.Application;
using InspectFlow.Modules.Tenants.Application;
using InspectFlow.Shared.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InspectFlow.Infrastructure.Seed;

/// <summary>
/// Applies migrations (when enabled), ensures the base roles exist and — in Development only —
/// creates demo data through the real application services (so seeded data obeys every business rule).
/// </summary>
public static class DatabaseInitializer
{
    public const string DemoPassword = "Demo@12345";
    public const string CompanyEmail = "company@demo.local";
    public const string AgentEmail = "agent@demo.local";
    public const string TenantEmail = "tenant@demo.local";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var env = sp.GetRequiredService<IHostEnvironment>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var db = sp.GetRequiredService<AppDbContext>();

        if (config.GetValue("Database:MigrateOnStartup", false))
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync(ct);
        }

        var roles = sp.GetRequiredService<RoleManager<Role>>();
        foreach (var role in AppRoles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new Role(role));

        if (!config.GetValue("Seed:Enabled", false)) return;
        if (!env.IsDevelopment())
        {
            logger.LogWarning("Seed:Enabled is ignored outside the Development environment (demo credentials are never created in {Env}).", env.EnvironmentName);
            return;
        }
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == CompanyEmail.ToUpperInvariant(), ct)) return;

        logger.LogInformation("Seeding development demo data");
        await new DemoSeeder(services).RunAsync(ct);
    }

    private sealed class DemoSeeder(IServiceProvider root)
    {
        private static readonly (RoomType Type, string Name, string Asset)[] Rooms =
        [
            (RoomType.LivingRoom, "Living Room", "living-room"),
            (RoomType.Bedroom, "Bedroom 1", "bedroom"),
            (RoomType.Bedroom, "Bedroom 2", "bedroom"),
            (RoomType.Kitchen, "Kitchen", "kitchen"),
            (RoomType.Bathroom, "Bathroom", "bathroom"),
            (RoomType.Garage, "Garage", "garage"),
        ];

        public async Task RunAsync(CancellationToken ct)
        {
            var company = await RegisterAsync(CompanyEmail, "Demo Company Owner", AppRoles.Company, ct);
            var agent = await RegisterAsync(AgentEmail, "Demo Inspector", AppRoles.Agent, ct);
            var tenant = await RegisterAsync(TenantEmail, "Demo Tenant", AppRoles.Tenant, ct);

            await As(company, AppRoles.Company, sp => sp.GetRequiredService<CompanyService>()
                .CreateWorkspaceAsync(new CreateCompanyRequest("Demo Property Management", "hello@demo-pm.local", "+353 1 555 0100"), ct));

            // Property 1: open public Move In waiting for an agent (marketplace demo).
            var (mainStreet, mainTenancy) = await CreatePropertyWithTenancyAsync(company, tenant, "12 Main Street", "D02 XY45", ct);
            await As(company, AppRoles.Company, sp => sp.GetRequiredService<InspectionService>().CreateAsync(
                new CreateInspectionRequest(mainStreet, mainTenancy, InspectionType.MoveIn, InspectionVisibility.Public, null,
                    "Keys are with the concierge. Please photograph every room from two corners.", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                    null, PublishNow: true, InviteEmail: null), ct));

            // Property 2: finalized and accepted Move In, ready for a Move Out comparison demo.
            var (oakAvenue, oakTenancy) = await CreatePropertyWithTenancyAsync(company, tenant, "48 Oak Avenue", "D04 AB12", ct);
            var moveIn = await As(company, AppRoles.Company, sp => sp.GetRequiredService<InspectionService>().CreateAsync(
                new CreateInspectionRequest(oakAvenue, oakTenancy, InspectionType.MoveIn, InspectionVisibility.Public, null,
                    null, null, null, PublishNow: true, InviteEmail: null), ct));
            var inspectionId = moveIn.Inspection.Id;

            await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().AcceptAsync(inspectionId, ct));
            var started = await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().StartAsync(inspectionId, ct));

            foreach (var room in started.Rooms)
            {
                var asset = Rooms.First(r => r.Name == room.Name).Asset;
                foreach (var variant in new[] { 1, 2 })
                    await UploadAsync(agent, inspectionId, room.Id, MediaType.General, null, $"{asset}-{variant}.jpg", ct);

                await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().UpdateRoomAsync(inspectionId, room.Id,
                    new UpdateRoomRequest(Description(room.Name), room.Name == "Living Room", null), ct));

                if (room.Name == "Living Room")
                {
                    var withDefect = await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>()
                        .AddDefectAsync(inspectionId, room.Id, new AddDefectRequest("Scuff marks on wall", "Wall to the left of the window"), ct));
                    var defectId = withDefect.Defects[0].Id;
                    await UploadAsync(agent, inspectionId, room.Id, MediaType.Defect, defectId, "defect-scuff.jpg", ct);
                    await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().UpdateDefectAsync(inspectionId, room.Id, defectId,
                        new UpdateDefectRequest("Scuff marks on wall", "Wall to the left of the window",
                            "Several light grey scuff marks, approx. 30 cm wide, on the painted wall to the left of the window.",
                            DefectClassification.PreExisting, AgentConfirmed: true), ct));
                }

                await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().CompleteRoomAsync(inspectionId, room.Id, ct));
            }

            await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<AgentInspectionService>().SubmitForReviewAsync(inspectionId, ct));
            await As(agent, AppRoles.Agent, sp => sp.GetRequiredService<FinalizationService>().FinalizeAsync(inspectionId, ct));
            await As(tenant, AppRoles.Tenant, sp => sp.GetRequiredService<TenantService>().AcceptAsync(inspectionId,
                new TenantDecisionRequest("Everything is correct."), ct));
        }

        private async Task<(Guid PropertyId, Guid TenancyId)> CreatePropertyWithTenancyAsync(Guid company, Guid tenant, string address,
            string postcode, CancellationToken ct)
        {
            var property = await As(company, AppRoles.Company, sp => sp.GetRequiredService<PropertyService>().CreateAsync(
                new CreatePropertyRequest(address, null, "Dublin", postcode, "Ireland", PropertyType.House,
                    Rooms.Select(r => new RoomInput(r.Type, r.Name)).ToList()), ct));
            var tenancy = await As(company, AppRoles.Company, sp => sp.GetRequiredService<TenancyService>().CreateAsync(
                new CreateTenancyRequest(property.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), $"TEN-{address[..2].Trim()}"), ct));
            var invite = await As(company, AppRoles.Company, sp => sp.GetRequiredService<TenancyService>().InviteTenantAsync(
                tenancy.Id, new InviteTenantRequest(TenantEmail, "Demo Tenant"), ct));
            var token = invite.InvitationLink[(invite.InvitationLink.LastIndexOf('/') + 1)..];
            await As(tenant, AppRoles.Tenant, sp => sp.GetRequiredService<TenancyService>().AcceptInvitationAsync(token, ct));
            return (property.Id, tenancy.Id);
        }

        private Task UploadAsync(Guid agent, Guid inspectionId, Guid roomId, MediaType type, Guid? defectId, string asset, CancellationToken ct) =>
            As(agent, AppRoles.Agent, async sp =>
            {
                await using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream($"InspectFlow.Infrastructure.Seed.Assets.{asset}")
                                         ?? throw new InvalidOperationException($"Missing seed asset {asset}");
                return await sp.GetRequiredService<MediaService>().UploadAsync(
                    new UploadMediaCommand(inspectionId, roomId, type, defectId, stream, asset, "image/jpeg", stream.Length), ct);
            });

        private async Task<Guid> RegisterAsync(string email, string name, string role, CancellationToken ct)
        {
            var result = await As(Guid.Empty, role, sp => sp.GetRequiredService<AuthService>()
                .RegisterAsync(new RegisterRequest(email, DemoPassword, name, role), ct));
            return result.User.Id;
        }

        private async Task<T> As<T>(Guid userId, string role, Func<IServiceProvider, Task<T>> action)
        {
            await using var scope = root.CreateAsyncScope();
            if (userId != Guid.Empty)
                ((CurrentUser)scope.ServiceProvider.GetRequiredService<ICurrentUser>()).RunAs(userId, role);
            return await action(scope.ServiceProvider);
        }

        private static string Description(string room) => room switch
        {
            "Living Room" => "Walls painted white and in good visible condition apart from the scuff marks noted below. White ceiling with pendant light fitting. Light oak laminate floor in good visible condition. Double-glazed window with white frame. Radiator below the window.",
            "Kitchen" => "Fitted wall and base units with grey doors, laminate worktops in good visible condition. Stainless steel sink with mixer tap. Integrated oven, hob and extractor hood. Grey ceramic floor tiles; no visible cracks in the provided images.",
            "Bathroom" => "White bath with glass shower screen, toilet and wash basin, all appearing clean. White wall tiles around the bath with intact grout visible. Grey vinyl floor in good visible condition. Extractor fan fitted.",
            "Garage" => "Concrete floor with light oil staining near the entrance. Painted block walls. Up-and-over door operates. Wall-mounted shelving and strip light.",
            _ => "Walls painted white and in good visible condition. White ceiling with light fitting. Beige carpet in good visible condition. Double-glazed window with curtain rail. No visible damage is apparent in the provided images.",
        };
    }
}
