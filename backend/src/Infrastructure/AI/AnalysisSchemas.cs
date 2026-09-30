using System.Text.Json.Nodes;

namespace InspectFlow.Infrastructure.AI;

/// <summary>JSON Schemas for OpenAI structured outputs (strict mode: every property required, no extras).</summary>
internal static class AnalysisSchemas
{
    private static JsonObject NullableString() => new() { ["type"] = new JsonArray("string", "null") };
    private static JsonObject Str() => new() { ["type"] = "string" };

    private static JsonObject Obj(params (string Name, JsonNode Schema)[] props)
    {
        var properties = new JsonObject();
        foreach (var (name, schema) in props) properties[name] = schema;
        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray(props.Select(p => (JsonNode)JsonValue.Create(p.Name)!).ToArray()),
            ["properties"] = properties,
        };
    }

    private static JsonObject Surface() => Obj(
        ("color", NullableString()),
        ("material", NullableString()),
        ("condition", new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("good", "fair", "poor", "not_visible") }),
        ("notes", NullableString()));

    private static JsonObject ArrayOf(JsonNode items) => new() { ["type"] = "array", ["items"] = items };

    public static JsonObject Room() => Obj(
        ("roomType", Str()),
        ("walls", Surface()),
        ("ceiling", Surface()),
        ("floor", Surface()),
        ("visibleItems", ArrayOf(Obj(("name", Str()), ("condition", NullableString()), ("notes", NullableString())))),
        ("observedDefects", ArrayOf(Obj(("location", Str()), ("description", Str()), ("severity", NullableString())))),
        ("cleanliness", NullableString()),
        ("limitations", ArrayOf(Str())),
        ("description", Str()));

    public static JsonObject Defect() => Obj(
        ("summary", Str()),
        ("description", Str()),
        ("location", NullableString()),
        ("confidence", new JsonObject { ["type"] = "number" }));

    public static JsonObject Comparison() => Obj(
        ("possibleDifferences", ArrayOf(Obj(
            ("area", Str()),
            ("baseline", Str()),
            ("current", Str()),
            ("suggestedClassification", new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("NewDamage", "PreExisting", "NormalWear", "Resolved", "Unchanged", "UnableToDetermine"),
            })))),
        ("summary", Str()),
        ("confidence", new JsonObject { ["type"] = "number" }));
}
