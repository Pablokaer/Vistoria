using InspectFlow.Modules.Billing.Domain;
using InspectFlow.Modules.Common;
using InspectFlow.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InspectFlow.Modules.Billing.Application;

/// <summary>
/// Answers "may this user use the paid product right now?" from the database on every call (never from token
/// claims, which would stay stale for the access-token lifetime after a cancellation).
/// </summary>
public sealed class SubscriptionAccessService(IAppDbContext db, IClock clock, IOptions<BillingOptions> options)
{
    private readonly BillingOptions _options = options.Value;

    /// <summary>Example: <c>access.IsRequiredFor(["Company"]) // true with the default configuration</c></summary>
    public bool IsRequiredFor(IEnumerable<string> roles) =>
        roles.Any(r => _options.SubscriptionRequiredRoles.Contains(r, StringComparer.Ordinal));

    /// <summary>Example: <c>(await access.GetAccessAsync(userId, ct)).HasAccess</c></summary>
    public async Task<SubscriptionAccess> GetAccessAsync(Guid userId, CancellationToken ct)
    {
        var current = await FindCurrentAsync(userId, ct);
        return SubscriptionAccessPolicy.Evaluate(current, clock.UtcNow, _options.Grace);
    }

    /// <summary>The open subscription (Pending/Active/PastDue) if any, otherwise the most recent ended one.</summary>
    public Task<Subscription?> FindCurrentAsync(Guid userId, CancellationToken ct) =>
        db.Subscriptions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => Subscription.OpenStatuses.Contains(s.Status))
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    /// <summary>Example: <c>var summary = await access.GetSummaryAsync(user.Id, ["Company"], ct);</c></summary>
    public async Task<SubscriptionSummaryDto> GetSummaryAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken ct)
    {
        var current = await FindCurrentAsync(userId, ct);
        var access = SubscriptionAccessPolicy.Evaluate(current, clock.UtcNow, _options.Grace);
        var required = IsRequiredFor(roles);
        var plan = current is null ? null : _options.FindPlan(current.PlanCode);
        return new SubscriptionSummaryDto(required, access.HasAccess || !required, access.EffectiveStatus,
            current?.PlanCode, plan?.Name, current?.CurrentPeriodEnd, access.AccessEndsAt);
    }
}
