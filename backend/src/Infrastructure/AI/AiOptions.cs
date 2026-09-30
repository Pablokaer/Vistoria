namespace InspectFlow.Infrastructure.AI;

public sealed class AiOptions
{
    /// <summary>"Auto" (OpenAI when a key is configured, otherwise Mock in Development), "OpenAI" or "Mock".</summary>
    public string Provider { get; set; } = "Auto";

    /// <summary>Run analyses synchronously within the request (tests / debugging).</summary>
    public bool ProcessInline { get; set; }

    /// <summary>Mock output outside Development must be an explicit decision.</summary>
    public bool AllowMockOutsideDevelopment { get; set; }

    public int MaxConcurrency { get; set; } = 2;

    public OpenAiOptions OpenAI { get; set; } = new();
}

public sealed class OpenAiOptions
{
    /// <summary>Only from environment (OPENAI_API_KEY) or secret store. Never logged, never sent to clients.</summary>
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4.1-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSeconds { get; set; } = 90;

    /// <summary>"low" | "high" | "auto" — image detail sent to the model.</summary>
    public string ImageDetail { get; set; } = "auto";
}
