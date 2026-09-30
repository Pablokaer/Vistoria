using InspectFlow.Modules.Inspections.Domain;
using InspectFlow.Modules.Properties.Domain;
using InspectFlow.Shared.Errors;

namespace InspectFlow.Tests.Domain;

public class RoomCompletionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Agent = Guid.NewGuid();

    private static InspectionRoom Room() => new() { Id = Guid.NewGuid(), Name = "Kitchen", RoomType = RoomType.Kitchen, Status = InspectionRoomStatus.Pending };

    private static RoomCompletionContext Ctx(int photos = 1, Dictionary<Guid, int>? defectPhotos = null, int pendingAnalyses = 0,
        bool comparisonRequired = false, bool decided = false) =>
        new(photos, defectPhotos ?? new Dictionary<Guid, int>(), 0, pendingAnalyses, comparisonRequired, decided);

    [Fact]
    public void Requires_photo_and_description()
    {
        var room = Room();
        var issues = room.GetCompletionIssues(Ctx(photos: 0));
        Assert.Equal(2, issues.Count);
        var ex = Assert.Throws<DomainRuleException>(() => room.Complete(Ctx(photos: 0), Now));
        Assert.Equal("room.incomplete", ex.Code);

        room.EditFinalDescription("Clean and tidy.", Agent, Now);
        room.Complete(Ctx(), Now);
        Assert.Equal(InspectionRoomStatus.Completed, room.Status);
    }

    [Fact]
    public void Defects_need_description_photo_and_confirmation()
    {
        var room = Room();
        room.EditFinalDescription("Room text", Agent, Now);
        room.SetDefectsFound(true, Now);
        Assert.Contains(room.GetCompletionIssues(Ctx()), i => i.Contains("no defect has been recorded", StringComparison.Ordinal));

        var defect = room.AddDefect("Scratch", "Door", Agent, Now);
        var issues = room.GetCompletionIssues(Ctx());
        Assert.Contains(issues, i => i.Contains("needs a description", StringComparison.Ordinal));
        Assert.Contains(issues, i => i.Contains("needs at least one photo", StringComparison.Ordinal));
        Assert.Contains(issues, i => i.Contains("confirmed", StringComparison.Ordinal));

        defect.Update("Scratch", "Door", "Visible scratch on the door.", DefectClassification.PreExisting, agentConfirmed: true, Now);
        Assert.Empty(room.GetCompletionIssues(Ctx(defectPhotos: new() { [defect.Id] = 1 })));
    }

    [Fact]
    public void Running_ai_analysis_and_missing_comparison_decision_block_completion()
    {
        var room = Room();
        room.EditFinalDescription("Room text", Agent, Now);
        Assert.Contains(room.GetCompletionIssues(Ctx(pendingAnalyses: 1)), i => i.Contains("AI analysis", StringComparison.Ordinal));
        Assert.Contains(room.GetCompletionIssues(Ctx(comparisonRequired: true)), i => i.Contains("Move In", StringComparison.Ordinal));
        Assert.Empty(room.GetCompletionIssues(Ctx(comparisonRequired: true, decided: true)));
    }

    [Fact]
    public void Ai_description_prefills_but_never_overwrites_agent_text_and_is_kept_separately()
    {
        var room = Room();
        room.ApplyAiDescription(Guid.NewGuid(), "AI text v1", Now);
        Assert.Equal("AI text v1", room.FinalDescription);
        Assert.Equal(DescriptionSource.Ai, room.FinalDescriptionSource);

        room.EditFinalDescription("Agent text", Agent, Now);
        room.ApplyAiDescription(Guid.NewGuid(), "AI text v2", Now);
        Assert.Equal("Agent text", room.FinalDescription);
        Assert.Equal("AI text v2", room.AiDescription);
        Assert.Equal(DescriptionSource.Agent, room.FinalDescriptionSource);
    }

    [Fact]
    public void Ai_defect_text_never_counts_as_agent_confirmation()
    {
        var room = Room();
        var defect = room.AddDefect(null, null, Agent, Now);
        defect.ApplyAiDescription(Guid.NewGuid(), "Possible crack", 0.8m, Now);
        Assert.False(defect.AgentConfirmed);
        Assert.Equal("Possible crack", defect.AIDescription);
    }

    [Fact]
    public void Cannot_uncheck_defects_found_while_defects_exist()
    {
        var room = Room();
        room.AddDefect("Stain", null, Agent, Now);
        Assert.Throws<DomainRuleException>(() => room.SetDefectsFound(false, Now));
    }
}
