using InspectFlow.Shared.Errors;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>
/// Single source of truth for allowed inspection status transitions.
/// Every status change in the domain goes through <see cref="EnsureCanTransition"/>.
/// </summary>
public static class InspectionStateMachine
{
    private static readonly Dictionary<InspectionStatus, InspectionStatus[]> Allowed = new()
    {
        [InspectionStatus.Draft] = [InspectionStatus.Open, InspectionStatus.Cancelled],
        [InspectionStatus.Open] = [InspectionStatus.Assigned, InspectionStatus.Cancelled, InspectionStatus.Expired],
        [InspectionStatus.Assigned] = [InspectionStatus.InProgress, InspectionStatus.Cancelled],
        [InspectionStatus.InProgress] = [InspectionStatus.Review, InspectionStatus.Cancelled],
        [InspectionStatus.Review] = [InspectionStatus.InProgress, InspectionStatus.Completed],
        [InspectionStatus.Completed] = [InspectionStatus.AwaitingTenant],
        [InspectionStatus.AwaitingTenant] = [InspectionStatus.Accepted, InspectionStatus.Disputed],
        [InspectionStatus.Accepted] = [],
        [InspectionStatus.Disputed] = [],
        [InspectionStatus.Cancelled] = [],
        [InspectionStatus.Expired] = [],
    };

    public static bool CanTransition(InspectionStatus from, InspectionStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<InspectionStatus> NextStates(InspectionStatus from) => Allowed[from];

    public static void EnsureCanTransition(InspectionStatus from, InspectionStatus to)
    {
        if (!CanTransition(from, to))
            throw new DomainRuleException("inspection.invalid_transition",
                $"An inspection cannot move from {from} to {to}.");
    }

    /// <summary>Statuses in which the report is final and immutable.</summary>
    public static bool IsFinalized(InspectionStatus status) =>
        status is InspectionStatus.Completed or InspectionStatus.AwaitingTenant
            or InspectionStatus.Accepted or InspectionStatus.Disputed;

    /// <summary>Statuses in which the assigned agent may change inspection content.</summary>
    public static bool IsEditable(InspectionStatus status) =>
        status is InspectionStatus.InProgress or InspectionStatus.Review;
}
