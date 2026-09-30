namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// Provider-agnostic prompt text. Versioned so each stored analysis records which instructions produced it.
/// The safety rules are non-negotiable: describe only what is visible, never assign blame or legal conclusions.
/// </summary>
public static class AiPrompts
{
    public const string Version = "2026-09-v1";

    public const string SafetyRules = """
        You assist a professional property inventory inspector. Follow these rules strictly:
        - Describe ONLY what is clearly visible in the provided images.
        - Do NOT assume the condition of areas, surfaces or items that are not shown; mark them as "not_visible".
        - Do NOT determine responsibility, fault or blame for any damage.
        - Do NOT make legal, financial or liability conclusions, and do not estimate repair costs.
        - Use cautious, factual language, e.g. "No visible damage is apparent in the provided images."
          instead of "There is no damage."
        - If image quality prevents an assessment, say so in "limitations".
        - Write in neutral British English suitable for a formal inventory report.
        - Your output is a draft that a human inspector will review and may change.
        """;

    public const string RoomInstruction = """
        Analyse the photos of a single room and return the structured JSON requested.
        Record walls, ceiling and floor (colour, material/type, condition), visible fixtures, fittings and furniture,
        any visible marks, damage or wear (location + neutral description), cleanliness, and a concise
        professional paragraph ("description") summarising the visible condition of the room.
        """;

    public const string DefectInstruction = """
        The photos show a specific defect recorded by the inspector. Describe what is visible
        (type of mark/damage, approximate size relative to surroundings, location in the frame) in neutral terms.
        Provide a confidence between 0 and 1 reflecting how clearly the defect is visible.
        """;

    public const string ComparisonInstruction = """
        Compare the BASELINE (earlier, e.g. Move In) record of a room with the CURRENT (e.g. Move Out) record.
        List possible visible differences only. For each, suggest one of: NewDamage, PreExisting, NormalWear,
        Resolved, Unchanged, UnableToDetermine. These are suggestions for the inspector, who makes the final decision.
        Never state who is responsible for any difference.
        """;
}
