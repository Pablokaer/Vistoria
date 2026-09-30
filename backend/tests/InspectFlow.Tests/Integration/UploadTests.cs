using System.Net;
using InspectFlow.Tests.Support;

namespace InspectFlow.Tests.Integration;

[Collection(ApiCollection.Name)]
public class UploadTests(TestApp app)
{
    private async Task<(ApiClient Agent, Guid InspectionId, List<Guid> Rooms)> StartedInspectionAsync()
    {
        var company = await TestData.CompanyAsync(app);
        var propertyId = await TestData.CreatePropertyAsync(company, ["Kitchen", "Bathroom"]);
        var tenancyId = await TestData.CreateTenancyAsync(company, propertyId);
        var inspectionId = TestData.InspectionId(await TestData.CreateInspectionAsync(company, propertyId, tenancyId));
        var agent = await app.CreateClient().RegisterAsync("Agent");
        return (agent, inspectionId, await TestData.AcceptAndStartAsync(agent, inspectionId));
    }

    [Fact]
    public async Task Valid_image_is_stored_and_served_through_a_signed_url_only()
    {
        var (agent, inspectionId, rooms) = await StartedInspectionAsync();
        var upload = (await agent.UploadAsync($"/api/agent/inspections/{inspectionId}/rooms/{rooms[0]}/photos", TestData.Jpeg(4096), "../../evil name.jpg", "image/jpeg")).EnsureOk();
        Assert.Equal("image/jpeg", upload["mimeType"].GetValue<string>());
        Assert.Equal("evil name.jpg", upload["originalFilename"].GetValue<string>());
        var url = upload["url"].GetValue<string>();
        Assert.Contains("sig=", url, StringComparison.Ordinal);

        var anonymous = app.Factory.CreateClient();
        var file = await anonymous.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal(4096, (await file.Content.ReadAsByteArrayAsync()).Length);

        var tampered = url.Replace("sig=", "sig=x", StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(tampered)).StatusCode);
        var otherKey = url.Replace("/rooms/", "/rooms/0", StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(otherKey)).StatusCode);
    }

    [Fact]
    public async Task Rejects_non_images_spoofed_types_oversized_files_and_foreign_rooms()
    {
        var (agent, inspectionId, rooms) = await StartedInspectionAsync();
        var url = $"/api/agent/inspections/{inspectionId}/rooms/{rooms[0]}/photos";

        var html = "<html><script>alert(1)</script></html>"u8.ToArray();
        (await agent.UploadAsync(url, html, "photo.jpg", "image/jpeg")).EnsureStatus(HttpStatusCode.BadRequest);

        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };
        (await agent.UploadAsync(url, png, "photo.jpg", "image/jpeg")).EnsureStatus(HttpStatusCode.BadRequest);

        (await agent.UploadAsync(url, TestData.Jpeg((int)TestApp.MaxUploadBytes + 10), "big.jpg", "image/jpeg")).EnsureStatus(HttpStatusCode.BadRequest);

        // A room id from another inspection.
        var (_, otherInspection, otherRooms) = await StartedInspectionAsync();
        (await agent.UploadAsync($"/api/agent/inspections/{inspectionId}/rooms/{otherRooms[0]}/photos", TestData.Jpeg(), "a.jpg", "image/jpeg"))
            .EnsureStatus(HttpStatusCode.NotFound);
        (await agent.UploadAsync($"/api/agent/inspections/{otherInspection}/rooms/{otherRooms[0]}/photos", TestData.Jpeg(), "a.jpg", "image/jpeg"))
            .EnsureStatus(HttpStatusCode.NotFound);

        // Defect photos must reference a defect of the same room.
        (await agent.UploadAsync(url, TestData.Jpeg(), "d.jpg", "image/jpeg", mediaType: "Defect")).EnsureStatus(HttpStatusCode.BadRequest);
        (await agent.UploadAsync(url, TestData.Jpeg(), "d.jpg", "image/jpeg", mediaType: "Defect", defectId: Guid.NewGuid())).EnsureStatus(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Uploads_are_rejected_after_finalization()
    {
        var (agent, inspectionId, rooms) = await StartedInspectionAsync();
        await TestData.CompleteAndFinalizeAsync(agent, inspectionId, rooms);
        var res = await agent.UploadAsync($"/api/agent/inspections/{inspectionId}/rooms/{rooms[0]}/photos", TestData.Jpeg(), "late.jpg", "image/jpeg");
        res.EnsureStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Equal("inspection.finalized", res.Code);
    }
}
