using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Tests.Domain;

public class InspectionStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Agent = Guid.NewGuid();

    public static Inspection NewDraft(InspectionType type = InspectionType.MoveIn, DateTimeOffset? acceptBy = null) =>
        Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), type, InspectionVisibility.Public, null, null, null,
            acceptBy, Guid.NewGuid(), Now);

    public static IReadOnlyList<RoomSnapshotSource> Rooms(int count = 2) =>
        Enumerable.Range(1, count).Select(i => new RoomSnapshotSource(Guid.NewGuid(), RoomType.Bedroom, $"Bedroom {i}", i)).ToList();

    [Theory]
    [InlineData(InspectionStatus.Draft, InspectionStatus.Open, true)]
    [InlineData(InspectionStatus.Open, InspectionStatus.Assigned, true)]
    [InlineData(InspectionStatus.Assigned, InspectionStatus.InProgress, true)]
    [InlineData(InspectionStatus.InProgress, InspectionStatus.Review, true)]
    [InlineData(InspectionStatus.Review, InspectionStatus.Completed, true)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.AwaitingTenant, true)]
    [InlineData(InspectionStatus.AwaitingTenant, InspectionStatus.Accepted, true)]
    [InlineData(InspectionStatus.AwaitingTenant, InspectionStatus.Disputed, true)]
    [InlineData(InspectionStatus.Draft, InspectionStatus.Assigned, false)]
    [InlineData(InspectionStatus.Open, InspectionStatus.InProgress, false)]
    [InlineData(InspectionStatus.InProgress, InspectionStatus.Completed, false)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.InProgress, false)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.Cancelled, false)]
    [InlineData(InspectionStatus.Accepted, InspectionStatus.Disputed, false)]
    [InlineData(InspectionStatus.Cancelled, InspectionStatus.Open, false)]
    public void Only_declared_transitions_are_allowed(InspectionStatus from, InspectionStatus to, bool allowed) =>
        Assert.Equal(allowed, InspectionStateMachine.CanTransition(from, to));

    [Fact]
    public void Happy_path_walks_through_every_state()
    {
        var inspection = NewDraft();
        inspection.Publish(Rooms(), Now);
        Assert.Equal(InspectionStatus.Open, inspection.Status);
        inspection.Accept(Agent, Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);
        Assert.Equal(Agent, inspection.AgentId);
        Assert.Equal(Now, inspection.AcceptedAt);
        inspection.Start(Agent, Now);
        inspection.SubmitForReview(Agent, [], Now);
        inspection.Finalize(Agent, [], Now);
        Assert.Equal(InspectionStatus.Completed, inspection.Status);
        Assert.Equal(Agent, inspection.CompletedBy);
        inspection.SendToTenant(Now);
        var tenant = Guid.NewGuid();
        inspection.RecordTenantDecision(accepted: true, tenant, Now);
        Assert.Equal(InspectionStatus.Accepted, inspection.Status);
        Assert.Equal(tenant, inspection.TenantRespondedBy);
    }

    [Fact]
    public void Cannot_accept_twice()
    {
        var inspection = NewDraft();
        inspection.Publish(Rooms(), Now);
        inspection.Accept(Agent, Now);
        var ex = Assert.Throws<DomainRuleException>(() => inspection.Accept(Guid.NewGuid(), Now));
        Assert.Equal("inspection.not_available", ex.Code);
        Assert.Equal(Agent, inspection.AgentId);
    }

    [Fact]
    public void Cannot_accept_draft_or_expired_inspection()
    {
        Assert.Throws<DomainRuleException>(() => NewDraft().Accept(Agent, Now));
        var expiring = NewDraft(acceptBy: Now.AddHours(1));
        expiring.Publish(Rooms(), Now);
        var ex = Assert.Throws<DomainRuleException>(() => expiring.Accept(Agent, Now.AddHours(2)));
        Assert.Equal("inspection.expired", ex.Code);
    }

    [Fact]
    public void Only_the_assigned_agent_can_start_and_edit()
    {
        var inspection = NewDraft();
        inspection.Publish(Rooms(), Now);
        inspection.Accept(Agent, Now);
        Assert.Throws<ForbiddenException>(() => inspection.Start(Guid.NewGuid(), Now));
        Assert.Throws<DomainRuleException>(() => inspection.EnsureEditableBy(Agent)); // not started yet
        inspection.Start(Agent, Now);
        inspection.EnsureEditableBy(Agent);
        Assert.Throws<ForbiddenException>(() => inspection.EnsureEditableBy(Guid.NewGuid()));
    }

    [Fact]
    public void Finalized_inspection_is_not_editable()
    {
        var inspection = NewDraft();
        inspection.Publish(Rooms(), Now);
        inspection.Accept(Agent, Now);
        inspection.Start(Agent, Now);
        inspection.SubmitForReview(Agent, [], Now);
        inspection.Finalize(Agent, [], Now);
        var ex = Assert.Throws<DomainRuleException>(() => inspection.EnsureEditableBy(Agent));
        Assert.Equal("inspection.finalized", ex.Code);
    }

    [Fact]
    public void Review_and_finalize_are_blocked_by_issues()
    {
        var inspection = NewDraft();
        inspection.Publish(Rooms(), Now);
        inspection.Accept(Agent, Now);
        inspection.Start(Agent, Now);
        var ex = Assert.Throws<DomainRuleException>(() => inspection.SubmitForReview(Agent, [new("Bedroom 1: not complete", "Quarto 1: não concluído")], Now));
        Assert.Equal("inspection.not_ready", ex.Code);
        Assert.Single(ex.Details);
        Assert.Equal(InspectionStatus.InProgress, inspection.Status);
        // Finalizing straight from InProgress is an invalid transition.
        Assert.Throws<DomainRuleException>(() => inspection.Finalize(Agent, [], Now));
    }

    [Fact]
    public void Publishing_requires_rooms_and_snapshots_them()
    {
        var inspection = NewDraft();
        Assert.Throws<DomainRuleException>(() => inspection.Publish([], Now));
        var rooms = Rooms(3);
        inspection.Publish(rooms, Now);
        Assert.Equal(3, inspection.Rooms.Count);
        Assert.All(inspection.Rooms, r => Assert.Equal(InspectionRoomStatus.Pending, r.Status));
        Assert.Equal(rooms.Select(r => r.PropertyRoomId), inspection.Rooms.OrderBy(r => r.Sequence).Select(r => r.OriginalPropertyRoomId));
        Assert.Throws<DomainRuleException>(() => inspection.Publish(rooms, Now)); // cannot publish twice
    }

    [Fact]
    public void Move_in_and_move_out_require_a_tenancy_and_only_move_out_or_periodic_can_compare()
    {
        Assert.Throws<ValidationException>(() => Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), null, InspectionType.MoveIn,
            InspectionVisibility.Public, null, null, null, null, Guid.NewGuid(), Now));
        Assert.Throws<ValidationException>(() => Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionType.MoveIn,
            InspectionVisibility.Public, Guid.NewGuid(), null, null, null, Guid.NewGuid(), Now));
        var moveOut = Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionType.MoveOut,
            InspectionVisibility.Public, Guid.NewGuid(), null, null, null, Guid.NewGuid(), Now);
        Assert.NotNull(moveOut.ComparisonInspectionId);
    }

    [Fact]
    public void Expire_only_after_deadline()
    {
        var inspection = NewDraft(acceptBy: Now.AddDays(1));
        inspection.Publish(Rooms(), Now);
        Assert.Throws<DomainRuleException>(() => inspection.Expire(Now));
        inspection.Expire(Now.AddDays(2));
        Assert.Equal(InspectionStatus.Expired, inspection.Status);
    }
}
