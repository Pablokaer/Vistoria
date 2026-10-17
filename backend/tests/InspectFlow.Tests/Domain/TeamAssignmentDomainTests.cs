using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Tests.Domain;

/// <summary>
/// Domain rules for who takes an inspection (ADR 0022): a draft starts Public, assigning makes it Direct (designated on
/// drafts, assigned at once otherwise), releasing makes it Public again, and nothing changes once the agent started.
/// </summary>
public class TeamAssignmentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AgentA = Guid.NewGuid();
    private static readonly Guid AgentB = Guid.NewGuid();

    private static Inspection Draft() =>
        Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionType.MoveIn, null, null, null,
            null, Guid.NewGuid(), Now);

    private static Inspection OpenPublicInspection()
    {
        var inspection = Draft();
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        return inspection;
    }

    [Fact]
    public void New_draft_is_public_and_publishes_open_to_the_team()
    {
        var inspection = Draft();
        Assert.Equal(InspectionVisibility.Public, inspection.Visibility);

        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Open, inspection.Status);
        Assert.Null(inspection.AgentId);
    }

    [Fact]
    public void Designated_draft_becomes_direct_and_is_assigned_on_publish()
    {
        var inspection = Draft();
        inspection.AssignToTeamAgent(AgentA, Now);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Equal(InspectionVisibility.Direct, inspection.Visibility);
        Assert.Equal(AgentA, inspection.AgentId);

        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);
        Assert.Equal(Now, inspection.AcceptedAt);
    }

    [Fact]
    public void Open_inspection_is_assigned_then_reassigned_before_start()
    {
        var inspection = OpenPublicInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);
        Assert.Equal(InspectionVisibility.Direct, inspection.Visibility);

        inspection.AssignToTeamAgent(AgentB, Now.AddHours(1));
        Assert.Equal(AgentB, inspection.AgentId);
        Assert.Equal(Now.AddHours(1), inspection.AcceptedAt);
    }

    [Fact]
    public void Accepted_public_inspection_can_be_taken_back_and_given_to_another_agent()
    {
        var inspection = OpenPublicInspection();
        inspection.Accept(AgentA, Now);

        inspection.ReleaseToTeam(Now);
        Assert.Equal(InspectionStatus.Open, inspection.Status);
        Assert.Equal(InspectionVisibility.Public, inspection.Visibility);
        inspection.Accept(AgentB, Now);
        Assert.Equal(AgentB, inspection.AgentId);
    }

    [Fact]
    public void Started_inspection_cannot_be_reassigned_or_released()
    {
        var inspection = OpenPublicInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.Start(AgentA, Now);

        var assign = Assert.Throws<DomainRuleException>(() => inspection.AssignToTeamAgent(AgentB, Now));
        Assert.Equal("inspection.not_assignable", assign.Code);
        var release = Assert.Throws<DomainRuleException>(() => inspection.ReleaseToTeam(Now));
        Assert.Equal("inspection.invalid_transition", release.Code);
        Assert.Equal(AgentA, inspection.AgentId);
    }

    [Fact]
    public void Release_returns_a_direct_inspection_to_the_team()
    {
        var inspection = OpenPublicInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.ReleaseToTeam(Now);

        Assert.Equal(InspectionStatus.Open, inspection.Status);
        Assert.Equal(InspectionVisibility.Public, inspection.Visibility);
        Assert.Null(inspection.AgentId);
        Assert.Null(inspection.AcceptedAt);
    }

    [Fact]
    public void Releasing_a_designated_draft_keeps_it_a_public_draft()
    {
        var inspection = Draft();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.ReleaseToTeam(Now);

        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Equal(InspectionVisibility.Public, inspection.Visibility);
        Assert.Null(inspection.AgentId);
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Open, inspection.Status);
    }

    [Fact]
    public void Release_without_an_agent_is_rejected_with_the_status()
    {
        var inspection = OpenPublicInspection();
        var ex = Assert.Throws<DomainRuleException>(() => inspection.ReleaseToTeam(Now));
        Assert.Equal("inspection.not_assigned", ex.Code);
        Assert.Contains("Open", ex.Text.En, StringComparison.Ordinal);
    }

    [Fact]
    public void Direct_inspection_cannot_be_accepted_by_a_colleague()
    {
        var inspection = Draft();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);

        var ex = Assert.Throws<DomainRuleException>(() => inspection.Accept(AgentB, Now));
        Assert.Equal("inspection.not_available", ex.Code);
        Assert.Equal(AgentA, inspection.AgentId);
    }

    [Fact]
    public void Editing_a_draft_keeps_the_designated_agent()
    {
        var inspection = Draft();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.UpdateDraft("Keys at the desk.", null, null, Now);

        Assert.Equal(AgentA, inspection.AgentId);
        Assert.Equal(InspectionVisibility.Direct, inspection.Visibility);
    }
}
