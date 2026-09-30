using System.Reflection;

namespace InspectFlow.Modules.AI.Application;

/// <summary>
/// The team-maintained writing guide (AI/Guidelines/inspection-writing-guide.md), embedded in the assembly.
/// Only the part below <see cref="ModelMarker"/> is sent to the model; the header is editing instructions for the team.
/// </summary>
public sealed record InspectionWritingGuide(string Version, string ModelText)
{
    public const string ResourceName = "InspectFlow.AI.InspectionWritingGuide";
    public const string ModelMarker = "=== GUIDE FOR THE MODEL ===";
    private const string VersionPrefix = "guide-version:";

    private static readonly Lazy<InspectionWritingGuide> Embedded = new(LoadEmbedded);

    /// <summary>The guide shipped with this build. Example: <c>InspectionWritingGuide.Current.Version // "2026-09-30.1"</c></summary>
    public static InspectionWritingGuide Current => Embedded.Value;

    /// <summary>
    /// Parses the guide text. Throws when the version line or the model marker is missing, so a broken edit
    /// fails the build's tests instead of silently sending an incomplete prompt.
    /// </summary>
    public static InspectionWritingGuide Parse(string markdown)
    {
        var firstLine = markdown.Split('\n', 2)[0].Trim();
        if (!firstLine.StartsWith(VersionPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Writing guide must start with \"{VersionPrefix} YYYY-MM-DD.n\"; found \"{firstLine}\".");
        var version = firstLine[VersionPrefix.Length..].Trim();

        // The marker must stand on its own line: the team notes quote it inline when explaining the split.
        var lines = markdown.Split('\n');
        var marker = Array.FindIndex(lines, l => l.Trim() == ModelMarker);
        if (marker < 0) throw new InvalidOperationException($"Writing guide {version} has no \"{ModelMarker}\" line separating team notes from model text.");
        var modelText = string.Join('\n', lines[(marker + 1)..]).Trim();
        if (modelText.Length == 0) throw new InvalidOperationException($"Writing guide {version} has no text after \"{ModelMarker}\".");
        return new InspectionWritingGuide(version, modelText);
    }

    private static InspectionWritingGuide LoadEmbedded()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing; check InspectFlow.Modules.csproj.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
