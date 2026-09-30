using InspectFlow.Infrastructure.Seed;
using InspectFlow.Modules.Identity.Application;

namespace InspectFlow.Api.Endpoints;

public sealed record DevAccount(string Email, string Role, string Label);

public sealed record DevSwitchRequest(string Email);

/// <summary>
/// Development-only account switcher for the seeded demo users. Mapped only when the environment is
/// Development, and it signs in through the normal login path, so it grants nothing the demo password does not.
/// </summary>
public static class DevEndpoints
{
    private static readonly DevAccount[] Accounts =
    [
        new(DatabaseInitializer.CompanyEmail, "Company", "Company — Demo Property Management"),
        new(DatabaseInitializer.AgentEmail, "Agent", "Agent — Demo Inspector"),
        new(DatabaseInitializer.TenantEmail, "Tenant", "Tenant — Demo Tenant"),
    ];

    public static void MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/dev").WithTags("Dev");

        g.MapGet("/accounts", () => Accounts);

        g.MapPost("/switch", async (DevSwitchRequest req, AuthService auth, HttpContext http, CancellationToken ct) =>
        {
            var account = Accounts.FirstOrDefault(a => string.Equals(a.Email, req.Email?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (account is null) return Results.NotFound();
            return AuthEndpoints.Respond(http, await auth.LoginAsync(new LoginRequest(account.Email, DatabaseInitializer.DemoPassword), ct));
        });
    }
}
