
namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// Provider-agnostic prompt text. Versioned so each stored analysis records which instructions produced it.
/// The safety rules are non-negotiable: describe only what is visible, never assign blame or legal conclusions.
/// </summary>
public static class AiPrompts
{
    // v2: output language comes from the company (was fixed British English) and the writing guide is appended.
    // v3: report voice (written as the inspector's report, no references to "the image"), walls and floor always
    //     covered, room overview first, defect close-ups among room photos recorded as defects.
    public const string Version = "2026-10-v3";

    /// <summary>
    /// Stored on every analysis (prompt_version): code prompt version + writing guide version + output language,
    /// e.g. "2026-09-v2+g2026-09-30.1+pt-BR", so quality can be compared per guide version and language.
    /// </summary>
    public static string FullVersion(AiOutputLanguage language) => $"{Version}+g{InspectionWritingGuide.Current.Version}+{language.Code}";

    public const string SafetyRules = """
        You assist a professional property inventory inspector. Follow these rules strictly:
        - Describe ONLY what is clearly visible in the provided images.
        - Do NOT assume the condition of areas, surfaces or items that are not shown; mark them as "not_visible".
        - Do NOT determine responsibility, fault or blame for any damage.
        - Do NOT make legal, financial or liability conclusions, and do not estimate repair costs.
        - Use cautious, factual inventory language, e.g. "No visible damage noted." or "in good visible condition"
          instead of "There is no damage." or "perfect condition".
        - If image quality prevents an assessment, say so in "limitations".
        - Write in a neutral, formal register suitable for a property inventory report.
        - Your output is a draft that a human inspector will review and may change.
        - Write the free text as the inspector's own report entry: never refer to yourself, to AI, or to "the
          image" / "the photo" ("This photo shows…", "In the image…"). Mention photos only in "limitations",
          when explaining why something could not be assessed.
        """;

    public const string RoomInstruction = """
        Analyse the photos of a single room and return the structured JSON requested, as the inspector who visited
        the room would record it.
        - Walls, ceiling and floor: always fill "walls", "ceiling" and "floor" (colour, material/type, condition).
          The walls and the floor must always be covered in "description"; if one is not shown clearly, use
          "not_visible" and say so in one short clause.
        - Record visible windows, doors, fixtures, fittings, appliances and furniture with their condition,
          and the cleanliness of the room.
        - Every visible mark, damage or wear goes into "observedDefects" (location + neutral description).
          If a photo is a close-up of a defect, record that defect in full there and mention it in "description".
        - "description" is the report paragraph for this room: start with one sentence describing the room as a
          whole (type of room, windows/natural light, furnished or unfurnished), then follow the writing guide's
          order and structure.
        """;

    public const string DefectInstruction = """
        The photos show a specific defect recorded by the inspector. Describe it as the inspector would in the
        report: what it is, the surface or item it is on (colour and material), where it is, its approximate size
        or count, and how it looks (colour, shape, depth, extent). Use the inspector's short label as the starting
        point, but describe only what the photos show; if they disagree, describe what is visible and note it.
        If the photos are too close to place the defect in the room, give only the location that is visible.
        Provide a confidence between 0 and 1 reflecting how clearly the defect is visible.
        """;

    public const string ComparisonInstruction = """
        Compare the BASELINE (earlier, e.g. Move In) record of a room with the CURRENT (e.g. Move Out) record.
        List possible visible differences only. For each, suggest one of: NewDamage, PreExisting, NormalWear,
        Resolved, Unchanged, UnableToDetermine. These are suggestions for the inspector, who makes the final decision.
        Never state who is responsible for any difference.
        """;

    /// <summary>
    /// Free text follows the company's report language; schema codes stay in English so they keep parsing.
    /// Example: <c>AiPrompts.LanguageRule(AiOutputLanguage.PortugueseBrazil)</c>
    /// </summary>
    public static string LanguageRule(AiOutputLanguage language) => $"""
        OUTPUT LANGUAGE: write every free-text value (descriptions, summaries, notes, item names, locations,
        limitations, colours and materials) in {language.PromptName} ({language.Code}), using the terminology a
        professional property inventory clerk in that language would use. Keep JSON keys, condition codes
        (good, fair, poor, not_visible) and classification codes exactly as specified in English.
        The examples in the writing guide are in English: follow their level of detail and structure, not their language.
        """;

    /// <summary>
    /// The full system message: non-negotiable safety rules, then the language rule, then the team's writing guide.
    /// Example: <c>var system = AiPrompts.SystemPrompt(AiOutputLanguage.English);</c>
    /// </summary>
    public static string SystemPrompt(AiOutputLanguage language) =>
        $"{SafetyRules}\n{LanguageRule(language)}\nWRITING GUIDE (the safety rules above take precedence):\n\n{InspectionWritingGuide.Current.ModelText}";
}
