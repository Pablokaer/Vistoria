using System.Text.Json;
using InspectFlow.Infrastructure.Pdf;
using InspectFlow.Modules.Reports.Application;

namespace InspectFlow.Tests.Domain;

public class ReportPdfTextTests
{
    private static readonly DateTimeOffset Moment = new(2026, 9, 30, 14, 5, 0, TimeSpan.Zero);

    [Fact]
    public void English_keeps_the_pre_localization_date_formats()
    {
        var text = ReportPdfText.For("en");
        Assert.Equal("30 Sep 2026 14:05", text.ShortDateTime(Moment));
        Assert.Equal("30 September 2026", text.LongDate(Moment));
        Assert.Equal(("Move In", "Inspection Report"), (text.Label("MoveIn"), text.InspectionReport));
    }

    [Fact]
    public void Portuguese_uses_Brazilian_dates_and_labels_without_a_culture()
    {
        var text = ReportPdfText.For("pt-BR");
        Assert.Equal("30 set. 2026 14:05", text.ShortDateTime(Moment));
        Assert.Equal("30 de setembro de 2026", text.LongDate(Moment));
        Assert.Equal(("Saída", "Laudo de Vistoria"), (text.Label("MoveOut"), text.InspectionReport));
        Assert.Equal("Sala de estar", text.Label("LivingRoom"));
    }

    [Fact]
    public void Unknown_languages_and_enum_values_fall_back_safely()
    {
        Assert.Equal("en", ReportPdfText.For("de-DE").Language);
        Assert.Equal("Balcony", ReportPdfText.For("en").Label("Balcony"));
    }

    [Fact]
    public void Every_text_is_filled_in_both_languages()
    {
        foreach (var language in new[] { "en", "pt-BR" })
        {
            var text = ReportPdfText.For(language);
            var empty = typeof(ReportPdfText).GetProperties().Where(p => p.PropertyType == typeof(string))
                .Where(p => p.Name != nameof(ReportPdfText.ShortMonthSuffix) && string.IsNullOrWhiteSpace((string?)p.GetValue(text)))
                .Select(p => p.Name).ToList();
            Assert.True(empty.Count == 0, $"{language} has empty PDF texts: {string.Join(", ", empty)}");
            Assert.Equal(12, text.MonthNames.Length);
        }
    }

    [Fact]
    public void Version_1_snapshots_without_a_language_read_as_English()
    {
        const string v1 = """{"schemaVersion":1,"reportId":"00000000-0000-0000-0000-000000000001","reportNumber":"R-1","versionNumber":1,"generatedAt":"2026-01-01T00:00:00+00:00","company":{"id":"00000000-0000-0000-0000-000000000002","name":"C"},"property":{"id":"00000000-0000-0000-0000-000000000003","addressLine1":"1 St","city":"X","postcode":"P","country":"IE","propertyType":"House"},"inspection":{"id":"00000000-0000-0000-0000-000000000004","type":"MoveIn","completedAt":"2026-01-01T00:00:00+00:00"},"tenants":[],"rooms":[]}""";
        var snapshot = JsonSerializer.Deserialize<ReportSnapshot>(v1, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal("en", snapshot.Language);
    }
}
