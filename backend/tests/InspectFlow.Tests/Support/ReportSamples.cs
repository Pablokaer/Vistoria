using InspectFlow.Modules.Reports.Application;

namespace InspectFlow.Tests.Support;

/// <summary>
/// Report snapshots for the PDF scenarios (no defects, defects, many photos, long texts, optional data missing,
/// Move Out comparison), with the real seed photos as image bytes. Named fake data, no database needed.
/// </summary>
public static class ReportSamples
{
    private static readonly DateTimeOffset Finalized = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

    /// <summary>Seed photos embedded in the Infrastructure assembly, keyed by file name (e.g. "kitchen-1.jpg").</summary>
    public static IReadOnlyDictionary<string, byte[]> SeedPhotos { get; } = LoadSeedPhotos();

    private static Dictionary<string, byte[]> LoadSeedPhotos()
    {
        var assembly = typeof(InspectFlow.Infrastructure.Pdf.QuestPdfReportService).Assembly;
        return assembly.GetManifestResourceNames().Where(n => n.EndsWith(".jpg", StringComparison.Ordinal))
            .ToDictionary(n => string.Join('.', n.Split('.')[^2..]), n =>
            {
                using var stream = assembly.GetManifestResourceStream(n)!;
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            });
    }

    private static ReportPhoto Photo(string file) => new(Guid.NewGuid(), file, "image/jpeg", null, Finalized, null);

    private static ReportDefect Defect(string title, string location, string classification, string description, params string[] photos) =>
        new(Guid.NewGuid(), title, location, classification, description, null, null, photos.Select(Photo).ToList());

    private static ReportRoom Room(int sequence, string name, string type, string? description, string[] photos,
        IReadOnlyList<ReportDefect>? defects = null, string? notes = null, ReportRoomComparison? comparison = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, type, sequence, description, null, null, "Agent", notes,
            defects is { Count: > 0 }, photos.Select(Photo).ToList(), defects ?? [], comparison);

    private static readonly string Walls = "Walls and ceiling painted white, in good visible condition with no marks. Light oak laminate floor, clean and intact. Window with white uPVC frame and working handle.";

    private static IReadOnlyList<ReportRoom> StandardRooms(bool withDefects) =>
    [
        Room(1, "Living Room", "LivingRoom", Walls, ["living-room-1.jpg", "living-room-2.jpg"],
            withDefects ? [Defect("Scuff marks", "Wall left of the window", "PreExisting", "Light grey scuff marks approx. 30 cm wide at skirting height.", "defect-scuff.jpg")] : null,
            withDefects ? "Tenant informed of the marks during the visit." : null),
        Room(2, "Bedroom 1", "Bedroom", Walls, ["bedroom-1.jpg"]),
        Room(3, "Bedroom 2", "Bedroom", Walls, ["bedroom-2.jpg", "bedroom-1.jpg", "living-room-2.jpg"]),
        Room(4, "Kitchen", "Kitchen", "Fitted white units with laminate worktop, stainless steel sink and mixer tap. Hob and oven clean.", ["kitchen-1.jpg", "kitchen-2.jpg", "kitchen-1.jpg", "kitchen-2.jpg"],
            withDefects ? [Defect("Chipped worktop", "Front edge next to the sink", "NewDamage", "Chip approx. 2 cm on the front edge of the worktop.", "kitchen-2.jpg", "kitchen-1.jpg")] : null),
        Room(5, "Bathroom", "Bathroom", "White suite (bath, basin, WC) with tiled surround. Grout clean, no visible cracks.", ["bathroom-1.jpg", "bathroom-2.jpg"]),
        Room(6, "Garage", "Garage", "Concrete floor with light oil staining, up-and-over door operating.", ["garage-1.jpg"],
            withDefects ? [Defect("Oil staining", "Centre of the floor", "NormalWear", "Light oil staining consistent with parking a car.", "garage-2.jpg")] : null),
    ];

    private static ReportSnapshot Snapshot(string type, IReadOnlyList<ReportRoom> rooms, bool withPeople = true,
        ReportComparisonSummary? comparison = null, string language = "en") => new(
        ReportSnapshot.CurrentSchemaVersion, Guid.Parse("7b0f2d8e-4c1a-4f55-9a3e-2d6c1b9e8a70"), "IR-202609-F89XVT3K", 1, Finalized,
        new ReportCompany(Guid.NewGuid(), "Demo Property Management", "hello@demo-pm.local", "+353 1 555 0100"),
        new ReportProperty(Guid.NewGuid(), "12 Main Street", withPeople ? "Apartment 4" : null, "Dublin", "D02 XY45", "Ireland", "House"),
        new ReportInspection(Guid.NewGuid(), type, new DateOnly(2026, 9, 30), Finalized.AddHours(-2), Finalized,
            withPeople ? Guid.NewGuid() : null, withPeople ? "TEN-12" : null,
            withPeople ? new DateOnly(2026, 9, 22) : null, withPeople ? new DateOnly(2027, 9, 29) : null,
            comparison is null ? null : Guid.NewGuid(), comparison?.SourceReportNumber, comparison is null ? null : Finalized.AddDays(-30)),
        withPeople ? new ReportPerson(Guid.NewGuid(), "Demo Inspector", "agent@demo.local") : null,
        withPeople ? [new ReportPerson(Guid.NewGuid(), "Demo Tenant", "tenant@demo.local")] : [],
        rooms, comparison, language);

    public static ReportSnapshot MoveInNoDefects() => Snapshot("MoveIn", StandardRooms(withDefects: false));

    public static ReportSnapshot MoveInWithDefects() => Snapshot("MoveIn", StandardRooms(withDefects: true));

    public static ReportSnapshot ManyPhotos() => Snapshot("MoveIn",
    [
        Room(1, "Living Room", "LivingRoom", Walls, Enumerable.Range(0, 9).Select(i => SeedPhotos.Keys.ElementAt(i % SeedPhotos.Count)).ToArray()),
        Room(2, "Kitchen", "Kitchen", Walls, ["kitchen-1.jpg", "kitchen-2.jpg", "kitchen-1.jpg", "kitchen-2.jpg", "kitchen-1.jpg"],
            [Defect("Chipped worktop", "Front edge", "NewDamage", "Chip approx. 2 cm.", "kitchen-2.jpg", "kitchen-1.jpg", "kitchen-2.jpg", "kitchen-1.jpg")]),
    ]);

    public static ReportSnapshot LongTexts()
    {
        var paragraph = string.Concat(Enumerable.Repeat(Walls + " ", 14));
        return Snapshot("Periodic",
        [
            Room(1, "Living Room with an unusually long name for layout testing", "LivingRoom", paragraph, ["living-room-1.jpg"],
                [Defect("Long defect", "Several places", "Unknown", paragraph, "defect-scuff.jpg")], paragraph),
            Room(2, "Bedroom 1", "Bedroom", paragraph, ["bedroom-1.jpg", "bedroom-2.jpg"]),
        ]);
    }

    /// <summary>No tenant, no tenancy, no inspector, a room without photos or description.</summary>
    public static ReportSnapshot OptionalDataMissing() => Snapshot("Other",
    [
        Room(1, "Hallway", "Hallway", null, []),
        Room(2, "Utility", "Utility", "Washing machine connection, no appliance present.", ["garage-2.jpg"]),
    ], withPeople: false);

    public static ReportSnapshot MoveOutComparison(string language = "en")
    {
        ReportRoomComparison Cmp(string before, string decision, string? notes, params string[] beforePhotos) =>
            new(Guid.NewGuid(), before, decision == "NewDamage" ? ["Scuff marks"] : [], beforePhotos.Select(Photo).ToList(),
                "Move In: 2 photos, walls white. Now: 2 photos. 1 possible difference identified.", "Possible new stain on the wall left of the window.", decision, notes);
        var rooms = new List<ReportRoom>
        {
            Room(1, "Living Room", "LivingRoom", Walls + " Stain on the wall left of the window.", ["living-room-2.jpg", "living-room-1.jpg"],
                [Defect("Wall stain", "Wall left of the window", "NewDamage", "Brown stain approx. 20 cm above the sofa.", "defect-scuff.jpg")], null,
                Cmp(Walls, "NewDamage", "Stain not present at Move In.", "living-room-1.jpg", "living-room-2.jpg")),
            Room(2, "Kitchen", "Kitchen", "Fitted white units, clean.", ["kitchen-1.jpg"], null, null, Cmp("Fitted white units, clean.", "Unchanged", "As at Move In.", "kitchen-2.jpg")),
            Room(3, "Garage", "Garage", "Concrete floor, light oil staining.", ["garage-1.jpg", "garage-2.jpg"], null, null, Cmp("Concrete floor.", "NormalWear", "Staining from normal use.", "garage-2.jpg")),
        };
        var items = rooms.Select(r => new ComparisonSummaryItem(r.Name, r.Comparison!.Decision!, r.Comparison.Notes)).ToList();
        var summary = new ReportComparisonSummary(Guid.NewGuid(), "IR-202608-A1B2C3D4", items.Count,
            items.GroupBy(i => i.Decision).ToDictionary(g => g.Key, g => g.Count()), items);
        return Snapshot("MoveOut", rooms, comparison: summary, language: language);
    }

    /// <summary>All scenarios by file-friendly name.</summary>
    public static IReadOnlyDictionary<string, Func<ReportSnapshot>> All { get; } = new Dictionary<string, Func<ReportSnapshot>>
    {
        ["move-in-no-defects"] = MoveInNoDefects,
        ["move-in-with-defects"] = MoveInWithDefects,
        ["many-photos"] = ManyPhotos,
        ["long-texts"] = LongTexts,
        ["optional-missing"] = OptionalDataMissing,
        ["move-out-comparison"] = () => MoveOutComparison(),
        ["move-out-comparison-pt-BR"] = () => MoveOutComparison("pt-BR"),
    };
}
