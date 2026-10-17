using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Tests.Domain;

/// <summary>
/// Domain rules for inspections handed to a team agent (Team and Private): designation on drafts, direct assignment,
/// reassignment and release (Team only).
/// </summary>
public class TeamAssignmentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AgentA = Guid.NewGuid();
    private static readonly Guid AgentB = Guid.NewGuid();

    private static Inspection TeamDraft(InspectionVisibility visibility = InspectionVisibility.Team) =>
        Inspection.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InspectionType.MoveIn, visibility, null, null, null,
            null, Guid.NewGuid(), Now);

    private static Inspection OpenTeamInspection()
    {
        var inspection = TeamDraft();
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        return inspection;
    }

    [Fact]
    public void Designated_draft_stays_draft_and_becomes_assigned_on_publish()
    {
        var inspection = TeamDraft();
        inspection.AssignToTeamAgent(AgentA, Now);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);
        Assert.Equal(AgentA, inspection.AgentId);

        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);
        Assert.Equal(Now, inspection.AcceptedAt);
    }

    [Fact]
    public void Open_team_inspection_is_assigned_then_reassigned_before_start()
    {
        var inspection = OpenTeamInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);

        inspection.AssignToTeamAgent(AgentB, Now.AddHours(1));
        Assert.Equal(AgentB, inspection.AgentId);
        Assert.Equal(Now.AddHours(1), inspection.AcceptedAt);
    }

    [Fact]
    public void Started_inspection_cannot_be_reassigned_or_released()
    {
        var inspection = OpenTeamInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.Start(AgentA, Now);

        var assign = Assert.Throws<DomainRuleException>(() => inspection.AssignToTeamAgent(AgentB, Now));
        Assert.Equal("inspection.not_assignable", assign.Code);
        var release = Assert.Throws<DomainRuleException>(() => inspection.ReleaseToTeam(Now));
        Assert.Equal("inspection.invalid_transition", release.Code);
    }

    [Fact]
    public void Public_inspections_reject_a_direct_assignment()
    {
        var inspection = TeamDraft(InspectionVisibility.Public);
        var ex = Assert.Throws<DomainRuleException>(() => inspection.AssignToTeamAgent(AgentA, Now));
        Assert.Equal("inspection.public_not_assignable", ex.Code);
        Assert.Contains("Public", ex.Text.En, StringComparison.Ordinal);
    }

    [Fact]
    public void Private_draft_sent_to_a_team_agent_is_assigned_on_publish()
    {
        var inspection = TeamDraft(InspectionVisibility.Private);
        inspection.AssignToTeamAgent(AgentA, Now);
        Assert.Equal(InspectionStatus.Draft, inspection.Status);

        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Assigned, inspection.Status);
        Assert.Equal(AgentA, inspection.AgentId);
    }

    [Fact]
    public void Private_inspection_switches_agent_but_cannot_be_released_to_the_team()
    {
        var inspection = TeamDraft(InspectionVisibility.Private);
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        inspection.AssignToTeamAgent(AgentB, Now.AddHours(1));
        Assert.Equal(AgentB, inspection.AgentId);

        var ex = Assert.Throws<DomainRuleException>(() => inspection.ReleaseToTeam(Now));
        Assert.Equal("inspection.not_team", ex.Code);
        Assert.Contains("Private", ex.Text.En, StringComparison.Ordinal);
    }

    [Fact]
    public void Changing_a_draft_between_team_and_private_keeps_the_designated_agent()
    {
        var inspection = TeamDraft();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.UpdateDraft(InspectionVisibility.Private, null, null, null, Now);

        Assert.Equal(AgentA, inspection.AgentId);
    }

    [Fact]
    public void Release_returns_an_assigned_inspection_to_the_team()
    {
        var inspection = OpenTeamInspection();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.ReleaseToTeam(Now);

        Assert.Equal(InspectionStatus.Open, inspection.Status);
        Assert.Null(inspection.AgentId);
        Assert.Null(inspection.AcceptedAt);
        inspection.Accept(AgentB, Now); // any team agent can take it again
        Assert.Equal(AgentB, inspection.AgentId);
    }

    [Fact]
    public void Changing_a_draft_away_from_team_drops_the_designated_agent()
    {
        var inspection = TeamDraft();
        inspection.AssignToTeamAgent(AgentA, Now);
        inspection.UpdateDraft(InspectionVisibility.Public, null, null, null, Now);

        Assert.Null(inspection.AgentId);
        inspection.Publish(InspectionStateMachineTests.Rooms(), Now);
        Assert.Equal(InspectionStatus.Open, inspection.Status);
    }
}
