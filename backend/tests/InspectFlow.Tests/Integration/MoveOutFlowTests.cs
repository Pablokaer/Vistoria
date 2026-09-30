using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class MoveOutFlowTests(TestApp app)
{
    [Fact]
    public async Task Move_out_compares_with_move_in_without_changing_it()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Living Room", "Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var tenant = await TestData.AddTenantAsync(app, company, tenancyId);

        // Baseline Move In (with AI analysis so the structured comparison has data).
        var moveInId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var moveInRooms = await TestData.AcceptAndStartAsync(agent, moveInId);
        foreach (var roomId in moveInRooms)
        {
            var url = $"/api/agent/inspections/{moveInId}/rooms/{roomId}";
            (await agent.UploadAsync($"{url}/photos", TestData.Jpeg(seed: 9), "in.jpg", "image/jpeg")).EnsureOk();
            (await agent.PostAsync($"{url}/analysis")).EnsureOk();
            (await agent.PostAsync($"{url}/complete")).EnsureOk();
        }
        (await agent.PostAsync($"/api/agent/inspections/{moveInId}/submit-review")).EnsureOk();
        (await agent.PostAsync($"/api/agent/inspections/{moveInId}/finalize")).EnsureOk();
        (await tenant.PostAsync($"/api/tenant/inspections/{moveInId}/accept")).EnsureOk();
        var moveInReport = (await company.GetAsync($"/api/inspections/{moveInId}/report")).EnsureOk();

        // Candidates and validation.
        var candidates = (await company.GetAsync($"/api/properties/{propertyId}/comparison-candidates")).EnsureOk();
        Assert.Contains(candidates.Body!.AsArray(), c => c!["id"]!.GetValue<string>() == moveInId.ToString());
        var otherProperty = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var otherTenancy = await TestData.CreateTenancyAsync(company, otherProperty);
        (await company.PostAsync("/api/inspections", new { propertyId = otherProperty, tenancyId = otherTenancy, inspectionType = "MoveOut", visibility = "Public", comparisonInspectionId = moveInId }))
            .EnsureStatus(HttpStatusCode.BadRequest);

        // Move Out linked to the Move In.
        var moveOut = await TestData.CreateInspectionAsync(company, propertyId, tenancyId, type: "MoveOut", comparisonInspectionId: moveInId);
        var moveOutId = TestData.InspectionId(moveOut);
        Assert.Equal(moveInId.ToString(), moveOut["inspection"]!["comparisonInspection"]!["inspectionId"]!.GetValue<string>());

        var agent2 = await app.CreateClient().RegisterAsync("Agent");
        var available = (await agent2.GetAsync("/api/agent/available")).EnsureOk();
        Assert.True(available.Body!.AsArray().Single(i => i!["id"]!.GetValue<string>() == moveOutId.ToString())!["hasComparison"]!.GetValue<bool>());
        var rooms = await TestData.AcceptAndStartAsync(agent2, moveOutId);

        var livingUrl = $"/api/agent/inspections/{moveOutId}/rooms/{rooms[0]}";
        var living = (await agent2.GetAsync(livingUrl)).EnsureOk();
        var baseline = living["comparison"]["baseline"];
        Assert.Equal(moveInReport["snapshot"]["rooms"][0]!["description"]!.GetValue<string>(), baseline["description"]!.GetValue<string>());
        Assert.Single(baseline["photoUrls"]!.AsArray());
        Assert.Null(living.Body!["comparison"]!["agentDecision"]);

        // New photos, a new damage, basic + AI comparison, decision.
        (await agent2.UploadAsync($"{livingUrl}/photos", TestData.Jpeg(seed: 42), "out.jpg", "image/jpeg")).EnsureOk();
        (await agent2.PostAsync($"{livingUrl}/analysis")).EnsureOk();
        living = (await agent2.PutAsync(livingUrl, new { finalDescription = "Wall now has a large stain.", defectsFound = true })).EnsureOk();
        living = (await agent2.PostAsync($"{livingUrl}/defects", new { description = "Stain on wall", location = "Above sofa" })).EnsureOk();
        var defectId = Guid.Parse(living["defects"][0]!["id"]!.GetValue<string>());
        (await agent2.UploadAsync($"{livingUrl}/photos", TestData.Jpeg(seed: 43), "stain.jpg", "image/jpeg", "Defect", defectId)).EnsureOk();
        (await agent2.PutAsync($"{livingUrl}/defects/{defectId}", new
        {
            description = "Stain on wall", location = "Above sofa", finalDescription = "Brown stain approx. 20 cm.", classification = "NewDamage", agentConfirmed = true,
        })).EnsureOk();

        // Completion requires the comparison decision.
        var blocked = await agent2.PostAsync($"{livingUrl}/complete");
        blocked.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Contains(blocked["details"].AsArray(), d => d!.GetValue<string>().Contains("Move In", StringComparison.Ordinal));

        var basic = (await agent2.PostAsync($"{livingUrl}/comparison/basic")).EnsureOk();
        var basicText = basic["comparison"]["basicComparison"].GetValue<string>();
        Assert.Contains("Stain on wall", basicText, StringComparison.Ordinal);
        Assert.Contains("possible difference", basicText, StringComparison.Ordinal);

        var aiComparison = (await agent2.PostAsync($"{livingUrl}/comparison/analysis")).EnsureOk();
        Assert.Equal("Completed", aiComparison["status"].GetValue<string>());
        living = (await agent2.GetAsync(livingUrl)).EnsureOk();
        Assert.NotNull(living["comparison"]["aiAnalysis"]);
        Assert.Null(living.Body!["comparison"]!["agentDecision"]); // AI never decides

        (await agent2.PutAsync($"{livingUrl}/comparison/decision", new { decision = "NewDamage", notes = "Stain not present at Move In." })).EnsureOk();
        (await agent2.PostAsync($"{livingUrl}/complete")).EnsureOk();

        var kitchenUrl = $"/api/agent/inspections/{moveOutId}/rooms/{rooms[1]}";
        (await agent2.UploadAsync($"{kitchenUrl}/photos", TestData.Jpeg(seed: 44), "k.jpg", "image/jpeg")).EnsureOk();
        (await agent2.PutAsync(kitchenUrl, new { finalDescription = "Kitchen as at Move In.", defectsFound = false })).EnsureOk();
        (await agent2.PutAsync($"{kitchenUrl}/comparison/decision", new { decision = "Unchanged" })).EnsureOk();
        (await agent2.PostAsync($"{kitchenUrl}/complete")).EnsureOk();

        (await agent2.PostAsync($"/api/agent/inspections/{moveOutId}/submit-review")).EnsureOk();
        var finalized = (await agent2.PostAsync($"/api/agent/inspections/{moveOutId}/finalize")).EnsureOk();
        Assert.Equal("AwaitingTenant", finalized["status"].GetValue<string>());

        // The Move Out report contains the comparison summary.
        var report = (await company.GetAsync($"/api/inspections/{moveOutId}/report")).EnsureOk();
        var summary = report["snapshot"]["comparison"];
        Assert.Equal(2, summary["roomsCompared"]!.GetValue<int>());
        Assert.Equal(1, summary["decisionCounts"]!["NewDamage"]!.GetValue<int>());
        Assert.Equal(moveInReport["reportNumber"].GetValue<string>(), summary["sourceReportNumber"]!.GetValue<string>());
        Assert.Equal("NewDamage", report["snapshot"]["rooms"][0]!["comparison"]!["decision"]!.GetValue<string>());
        var pdf = await app.Factory.CreateClient().GetByteArrayAsync(report["pdfUrl"].GetValue<string>());
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));

        // The Move In report is untouched.
        var moveInAfter = (await company.GetAsync($"/api/inspections/{moveInId}/report")).EnsureOk();
        Assert.Equal(moveInReport["snapshotSha256"].GetValue<string>(), moveInAfter["snapshotSha256"].GetValue<string>());
        Assert.Equal("Accepted", moveInAfter["inspectionStatus"].GetValue<string>());

        // Tenant reviews the Move Out.
        var tenantView = (await tenant.GetAsync($"/api/tenant/inspections/{moveOutId}/report")).EnsureOk();
        Assert.NotNull(tenantView["snapshot"]["comparison"]);
        (await tenant.PostAsync($"/api/tenant/inspections/{moveOutId}/dispute", new { comment = "The stain was there before." })).EnsureOk();
    }

    [Fact]
    public async Task Baseline_must_belong_to_the_same_tenancy()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var oldTenancy = await TestData.CreateTenancyAsync(company, propertyId);
        var moveIn = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, oldTenancy));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        await TestData.CompleteAndFinalizeAsync(agent, moveIn, await TestData.AcceptAndStartAsync(agent, moveIn));

        var newTenancy = await TestData.CreateTenancyAsync(company, propertyId);
        foreach (var type in new[] { "MoveOut", "Periodic" })
            (await company.PostAsync("/api/inspections", new { propertyId, tenancyId = newTenancy, inspectionType = type, visibility = "Public", comparisonInspectionId = moveIn }))
                .EnsureStatus(HttpStatusCode.BadRequest);
        (await company.PostAsync("/api/inspections", new { propertyId, tenancyId = oldTenancy, inspectionType = "Periodic", visibility = "Public", comparisonInspectionId = moveIn }))
            .EnsureOk();
    }

    [Fact]
    public async Task Move_out_must_reference_a_finalized_move_in()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var openMoveIn = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        (await company.PostAsync("/api/inspections", new { propertyId, tenancyId, inspectionType = "MoveOut", visibility = "Public", comparisonInspectionId = openMoveIn }))
            .EnsureStatus(HttpStatusCode.BadRequest);
    }
}
