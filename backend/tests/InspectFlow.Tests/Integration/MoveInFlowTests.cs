using System.Net;
using InspectFlow.Modules.Audit.Domain;
using InspectFlow.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InspectFlow.Tests.Integration;

/// <summary>The mandatory end-to-end Move In scenario, through the public HTTP API.</summary>
[Collection(ApiCollection.Name)]
public class MoveInFlowTests(TestApp app)
{
    [Fact]
    public async Task Full_move_in_flow_from_company_signup_to_tenant_acceptance()
    {
        // Company signs up, creates workspace, property with 6 rooms and a tenancy with a tenant.
        var company = await TestData.CompanyAsync(app, "Acme Lettings");
        var propertyId = await TestData.CreatePropertyAsync(company);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var tenant = await TestData.AddTenantAsync(app, company, tenancyId);

        // Company creates and publishes a public Move In.
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));

        // Agent finds, accepts and starts it.
        var agent = await app.CreateClient().RegisterAsync("Agent", name: "Ian Inspector");
        var available = (await agent.GetAsync("/api/agent/available")).EnsureOk();
        Assert.Contains(available.Body!.AsArray(), i => i!["id"]!.GetValue<string>() == inspectionId.ToString());
        var rooms = await TestData.AcceptAndStartAsync(agent, inspectionId);
        Assert.Equal(6, rooms.Count);

        var ai = (await agent.GetAsync("/api/ai/status")).EnsureOk();
        Assert.True(ai["isMock"].GetValue<bool>());

        // Living room: photos, AI description, agent edit, defect with AI + confirmation.
        var living = rooms[0];
        var roomUrl = $"/api/agent/inspections/{inspectionId}/rooms/{living}";
        (await agent.UploadAsync($"{roomUrl}/photos", TestData.Jpeg(seed: 1), "lr1.jpg", "image/jpeg")).EnsureOk();
        (await agent.UploadAsync($"{roomUrl}/photos", TestData.Jpeg(seed: 2), "lr2.jpg", "image/jpeg")).EnsureOk();

        var analysis = (await agent.PostAsync($"{roomUrl}/analysis")).EnsureOk();
        Assert.Equal("Completed", analysis["status"].GetValue<string>()); // inline processing in tests
        var room = (await agent.GetAsync(roomUrl)).EnsureOk();
        var aiText = room["aiDescription"].GetValue<string>();
        Assert.Contains("mock", aiText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No visible damage is apparent", aiText, StringComparison.Ordinal);
        Assert.Equal(aiText, room["finalDescription"].GetValue<string>());
        Assert.Equal("Ai", room["finalDescriptionSource"].GetValue<string>());

        const string edited = "Walls white, good visible condition. Laminate floor. Scuff marks noted below.";
        room = (await agent.PutAsync(roomUrl, new { finalDescription = edited, defectsFound = true, agentNotes = "Keys tested." })).EnsureOk();
        Assert.Equal(edited, room["finalDescription"].GetValue<string>());
        Assert.Equal(aiText, room["aiDescription"].GetValue<string>()); // AI original preserved
        Assert.Equal("Agent", room["finalDescriptionSource"].GetValue<string>());

        room = (await agent.PostAsync($"{roomUrl}/defects", new { description = "Scuff marks", location = "Left wall" })).EnsureOk();
        var defectId = Guid.Parse(room["defects"].AsArray()[0]!["id"]!.GetValue<string>());
        (await agent.UploadAsync($"{roomUrl}/photos", TestData.Jpeg(seed: 3), "scuff.jpg", "image/jpeg", "Defect", defectId)).EnsureOk();
        var defectAnalysis = (await agent.PostAsync($"{roomUrl}/defects/{defectId}/analysis")).EnsureOk();
        Assert.Equal("Completed", defectAnalysis["status"].GetValue<string>());

        // Completion is blocked until the agent confirms the defect.
        var blocked = await agent.PostAsync($"{roomUrl}/complete");
        blocked.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Contains(blocked["details"].AsArray(), d => d!.GetValue<string>().Contains("confirmed", StringComparison.Ordinal));

        room = (await agent.GetAsync(roomUrl)).EnsureOk();
        var defect = room["defects"].AsArray()[0]!;
        Assert.False(defect["agentConfirmed"]!.GetValue<bool>());
        Assert.NotNull(defect["aiDescription"]);
        (await agent.PutAsync($"{roomUrl}/defects/{defectId}", new
        {
            description = "Scuff marks",
            location = "Left wall",
            finalDescription = "Light scuff marks approx. 30 cm wide on the left wall.",
            classification = "PreExisting",
            agentConfirmed = true,
        })).EnsureOk();
        room = (await agent.PostAsync($"{roomUrl}/complete")).EnsureOk();
        Assert.Equal("Completed", room["status"].GetValue<string>());
        Assert.Equal(1, room["roomsCompleted"].GetValue<int>());

        // Review cannot start before every room is complete.
        var early = await agent.PostAsync($"/api/agent/inspections/{inspectionId}/submit-review");
        early.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal(5, early["details"].AsArray().Count);

        foreach (var roomId in rooms.Skip(1)) await TestData.CompleteRoomAsync(agent, inspectionId, roomId);
        var review = (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/submit-review")).EnsureOk();
        Assert.Equal("Review", review["status"].GetValue<string>());

        var preview = (await agent.GetAsync($"/api/agent/inspections/{inspectionId}/review")).EnsureOk();
        Assert.True(preview["canFinalize"].GetValue<bool>());
        Assert.Equal(6, preview["preview"]["rooms"].AsArray().Count);

        // Text can still be edited during review.
        (await agent.PutAsync($"/api/agent/inspections/{inspectionId}/rooms/{rooms[1]}",
            new { finalDescription = "Bedroom: carpet good visible condition.", defectsFound = false, agentNotes = (string?)null })).EnsureOk();

        // Finalize → report v1 + PDF, sent to tenant.
        var finalized = (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/finalize")).EnsureOk();
        Assert.Equal("AwaitingTenant", finalized["status"].GetValue<string>());
        Assert.StartsWith("IR-", finalized["reportNumber"].GetValue<string>(), StringComparison.Ordinal);
        Assert.Equal(1, finalized["version"].GetValue<int>());

        // Immutable afterwards.
        (await agent.PutAsync(roomUrl, new { finalDescription = "changed", defectsFound = true })).EnsureStatus(HttpStatusCode.UnprocessableEntity);
        (await agent.PostAsync($"/api/agent/inspections/{inspectionId}/finalize")).EnsureStatus(HttpStatusCode.UnprocessableEntity);

        // Company sees the finalized report; PDF is downloadable.
        var report = (await company.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();
        Assert.Equal("Company", report["viewerKind"].GetValue<string>());
        var snapshotRooms = report["snapshot"]["rooms"].AsArray();
        Assert.Equal(TestData.RoomNames, snapshotRooms.Select(r => r!["name"]!.GetValue<string>()));
        Assert.Equal(edited, snapshotRooms[0]!["description"]!.GetValue<string>());
        Assert.Equal("Light scuff marks approx. 30 cm wide on the left wall.", snapshotRooms[0]!["defects"]![0]!["description"]!.GetValue<string>());
        Assert.Equal("Tina Tenant", report["snapshot"]["tenants"][0]!["name"]!.GetValue<string>());
        Assert.Equal("Ian Inspector", report["snapshot"]["agent"]["name"].GetValue<string>());
        var pdf = await app.Factory.CreateClient().GetByteArrayAsync(report["pdfUrl"].GetValue<string>());
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(pdf.Length > 2000);

        // Tenant reviews: sees it on the dashboard, opens, comments on a room, accepts.
        var dashboard = (await tenant.GetAsync("/api/tenant/dashboard")).EnsureOk();
        Assert.Single(dashboard["awaitingReview"].AsArray());
        var tenantReport = (await tenant.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureOk();
        Assert.True(tenantReport["canRespond"].GetValue<bool>());
        var withComment = (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/observations",
            new { roomId = living, text = "The scuff marks were already there when I viewed the property." })).EnsureOk();
        Assert.Equal("Living Room", withComment["observations"][0]!["roomName"]!.GetValue<string>());
        (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/observations", new { text = "General: all keys received." })).EnsureOk();

        var accepted = (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/accept", new { comment = "Everything is correct" })).EnsureOk();
        Assert.Equal("Accepted", accepted["inspectionStatus"].GetValue<string>());
        Assert.False(accepted["canRespond"].GetValue<bool>());
        (await tenant.PostAsync($"/api/tenant/inspections/{inspectionId}/dispute", new { comment = "changed my mind" })).EnsureStatus(HttpStatusCode.UnprocessableEntity);

        var companyView = (await company.GetAsync($"/api/inspections/{inspectionId}")).EnsureOk();
        Assert.Equal("Accepted", companyView["status"].GetValue<string>());

        // Agents never receive tenant emails, even in the report snapshot.
        var agentReport = (await agent.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();
        Assert.Null(agentReport.Body!["snapshot"]!["tenants"]![0]!["email"]);

        // Audit trail.
        await using var db = app.CreateDbContext();
        // Tokenised invitation links are never persisted in notifications.
        Assert.DoesNotContain(await db.Notifications.AsNoTracking().Select(n => n.Link).ToListAsync(),
            l => l != null && (l.Contains("/tenant/invite/", StringComparison.Ordinal) || l.Contains("/inspection/invite/", StringComparison.Ordinal)));
        var actions = await db.AuditLogs.AsNoTracking().Where(a => a.EntityId == inspectionId.ToString() ||
            db.AiAnalyses.Any(x => x.InspectionId == inspectionId && x.Id.ToString() == a.EntityId) ||
            db.InspectionRoomMedia.Any(x => x.InspectionId == inspectionId && x.Id.ToString() == a.EntityId) ||
            db.InspectionRooms.Any(x => x.InspectionId == inspectionId && x.Id.ToString() == a.EntityId) ||
            db.InspectionReports.Any(x => x.InspectionId == inspectionId && x.Id.ToString() == a.EntityId))
            .Select(a => a.Action).Distinct().ToListAsync();
        foreach (var expected in new[]
                 {
                     AuditActions.InspectionCreated, AuditActions.InspectionPublished, AuditActions.InspectionAccepted, AuditActions.InspectionStarted,
                     AuditActions.PhotoUploaded, AuditActions.AIAnalysisRequested, AuditActions.AIAnalysisCompleted, AuditActions.DescriptionEdited,
                     AuditActions.RoomCompleted, AuditActions.InspectionFinalized, AuditActions.ReportGenerated, AuditActions.TenantViewed,
                     AuditActions.TenantCommented, AuditActions.TenantAccepted,
                 })
            Assert.Contains(expected, actions);
    }

    [Fact]
    public async Task Finalized_report_is_reproducible_and_protected_against_mutation()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen", "Bathroom"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var rooms = await TestData.AcceptAndStartAsync(agent, inspectionId);
        var finalized = await TestData.CompleteAndFinalizeAsync(agent, inspectionId, rooms);
        Assert.Equal("Completed", finalized["status"]!.GetValue<string>()); // no tenants → stays Completed

        var before = (await company.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();

        // Live data changes do not affect the report.
        var property = (await company.GetAsync($"/api/properties/{propertyId}")).EnsureOk();
        var kitchen = property["rooms"][0]["id"].GetValue<string>();
        (await company.PutAsync($"/api/properties/{propertyId}/rooms/{kitchen}", new { roomType = "Utility", name = "Utility Room" })).EnsureOk();
        (await company.PutAsync($"/api/properties/{propertyId}", new { addressLine1 = "99 New Road", city = "Cork", postcode = "T12", country = "Ireland", propertyType = "House" })).EnsureOk();

        var after = (await company.GetAsync($"/api/inspections/{inspectionId}/report")).EnsureOk();
        Assert.Equal(before["snapshotSha256"].GetValue<string>(), after["snapshotSha256"].GetValue<string>());
        Assert.Equal("12 Main Street", after["snapshot"]["property"]["addressLine1"].GetValue<string>());
        Assert.Equal("Kitchen", after["snapshot"]["rooms"][0]!["name"]!.GetValue<string>());

        await using var db = app.CreateDbContext();
        var version = await db.InspectionReportVersions.SingleAsync(v => db.InspectionReports.Any(r => r.Id == v.ReportId && r.InspectionId == inspectionId));
        Assert.Equal(InspectFlow.Shared.Security.SecureTokens.Sha256Hex(version.SnapshotJson), version.SnapshotSha256);

        // Application guard.
        version.Reason = "tampered";
        await Assert.ThrowsAsync<InspectFlow.Infrastructure.Persistence.ImmutableRecordException>(() => db.SaveChangesAsync());

        // Database trigger (bypassing EF).
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"UPDATE reports.inspection_report_versions SET reason = 'x' WHERE id = {version.Id}"));
        Assert.Contains("append-only", ex.MessageText, StringComparison.Ordinal);

        // Tenant added later → company sends the report to the tenant.
        var tenant = await TestData.AddTenantAsync(app, company, tenancyId);
        (await tenant.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureStatus(HttpStatusCode.NotFound);
        var sent = (await company.PostAsync($"/api/inspections/{inspectionId}/send-to-tenant")).EnsureOk();
        Assert.Equal("AwaitingTenant", sent["status"].GetValue<string>());
        (await tenant.GetAsync($"/api/tenant/inspections/{inspectionId}/report")).EnsureOk();
    }

    [Fact]
    public async Task Share_link_gives_read_only_access_without_personal_emails()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        await TestData.AddTenantAsync(app, company, tenancyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        var finalized = await TestData.CompleteAndFinalizeAsync(agent, inspectionId, await TestData.AcceptAndStartAsync(agent, inspectionId));
        var reportId = finalized["reportId"]!.GetValue<string>();

        var share = (await company.PostAsync($"/api/reports/{reportId}/share-links", new { days = 7 })).EnsureOk();
        var link = share["link"].GetValue<string>();
        var token = link[(link.LastIndexOf('/') + 1)..];
        var shared = (await app.CreateClient().GetAsync($"/api/shared/reports/{token}")).EnsureOk();
        Assert.Equal("Shared", shared["viewerKind"].GetValue<string>());
        Assert.DoesNotContain("@", shared["snapshot"]["tenants"].ToJsonString(), StringComparison.Ordinal);
        Assert.Null(shared.Body!["snapshot"]!["agent"]!["email"]);

        (await agent.PostAsync($"/api/reports/{reportId}/share-links")).EnsureStatus(HttpStatusCode.Forbidden);
        (await company.DeleteAsync($"/api/reports/{reportId}/share-links/{share.Id}")).EnsureStatus(HttpStatusCode.NoContent);
        (await app.CreateClient().GetAsync($"/api/shared/reports/{token}")).EnsureStatus(HttpStatusCode.NotFound);
        (await app.CreateClient().GetAsync($"/api/reports/{reportId}")).EnsureStatus(HttpStatusCode.Unauthorized);
    }
}
