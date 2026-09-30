namespace InspectFlow.Api.Infrastructure;

/// <summary>
/// Maps the documented, flat environment variable names (.env.example) onto configuration keys.
/// Standard ASP.NET Core names (e.g. Jwt__Secret) keep working too.
/// </summary>
public static class EnvironmentAliases
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        ["DATABASE_CONNECTION_STRING"] = "ConnectionStrings:Default",
        ["JWT_SECRET"] = "Jwt:Secret",
        ["JWT_ISSUER"] = "Jwt:Issuer",
        ["JWT_AUDIENCE"] = "Jwt:Audience",
        ["OPENAI_API_KEY"] = "Ai:OpenAI:ApiKey",
        ["OPENAI_MODEL"] = "Ai:OpenAI:Model",
        ["OPENAI_BASE_URL"] = "Ai:OpenAI:BaseUrl",
        ["AI_PROVIDER"] = "Ai:Provider",
        ["STORAGE_PROVIDER"] = "Storage:Provider",
        ["STORAGE_LOCAL_PATH"] = "Storage:Local:RootPath",
        ["STORAGE_SIGNING_KEY"] = "Storage:SigningKey",
        ["STORAGE_PUBLIC_BASE_URL"] = "Storage:PublicBaseUrl",
        ["WEB_BASE_URL"] = "App:WebBaseUrl",
        ["CORS_ALLOWED_ORIGINS"] = "Cors:AllowedOrigins",
        ["SEED_DEMO_DATA"] = "Seed:Enabled",
        ["MIGRATE_ON_STARTUP"] = "Database:MigrateOnStartup",
        ["TRUST_FORWARDED_HEADERS"] = "ForwardedHeaders:Enabled",
    };

    public static IEnumerable<KeyValuePair<string, string?>> FromEnvironment()
    {
        foreach (var (env, key) in Map)
        {
            var value = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrEmpty(value)) yield return new(key, value);
        }
    }
}
