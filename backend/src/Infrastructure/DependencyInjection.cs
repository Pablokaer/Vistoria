using InspectFlow.Infrastructure.AI;
using InspectFlow.Infrastructure.Background;
using InspectFlow.Infrastructure.Billing;
using InspectFlow.Infrastructure.Identity;
using InspectFlow.Infrastructure.Pdf;
using InspectFlow.Infrastructure.Persistence;
using InspectFlow.Infrastructure.Services;
using InspectFlow.Infrastructure.Storage;
using InspectFlow.Modules.AI.Application;
using InspectFlow.Modules.Audit.Application;
using InspectFlow.Modules.Billing.Application;
using InspectFlow.Modules.Common;
using InspectFlow.Modules.Companies.Application;
using InspectFlow.Modules.Identity.Application;
using InspectFlow.Modules.Identity.Domain;
using InspectFlow.Modules.Inspections.Application;
using InspectFlow.Modules.Media.Application;
using InspectFlow.Modules.Notifications.Application;
using InspectFlow.Modules.Properties.Application;
using InspectFlow.Modules.Reports.Application;
using InspectFlow.Modules.Tenancies.Application;
using InspectFlow.Modules.Tenants.Application;
using InspectFlow.Shared.Auth;
using InspectFlow.Shared.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InspectFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInspectFlow(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        // ---- Options ----
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<AppUrlOptions>(configuration.GetSection("App"));
        services.Configure<InspectionRulesOptions>(configuration.GetSection("Inspections"));
        services.Configure<BillingOptions>(configuration.GetSection("Billing"));
        services.Configure<StripeOptions>(configuration.GetSection("Billing:Stripe"));
        services.Configure<SandboxOptions>(configuration.GetSection("Billing:Sandbox"));
        services.AddOptions<BillingOptions>().Validate(o => o.Plans.Count > 0 && o.Plans.All(p => p.Code.Length > 0 && p.PriceCents > 0),
            "Billing:Plans must contain at least one plan with a Code and a PriceCents > 0 (there is no free plan).").ValidateOnStart();
        services.AddOptions<JwtOptions>().Validate(o => o.Secret.Length >= 32, "Jwt:Secret (JWT_SECRET) must be at least 32 characters.").ValidateOnStart();
        services.AddOptions<StorageOptions>().Validate(o => o.SigningKey.Length >= 32, "Storage:SigningKey (STORAGE_SIGNING_KEY) must be at least 32 characters.").ValidateOnStart();
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
        {
            // The docker-compose defaults are prefixed "dev-only-" and must never reach a real deployment.
            services.AddOptions<JwtOptions>().Validate(o => !o.Secret.StartsWith("dev-only", StringComparison.Ordinal), "JWT_SECRET uses a development default.").ValidateOnStart();
            services.AddOptions<StorageOptions>().Validate(o => !o.SigningKey.StartsWith("dev-only", StringComparison.Ordinal), "STORAGE_SIGNING_KEY uses a development default.").ValidateOnStart();
        }

        // ---- Persistence ----
        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("ConnectionStrings:Default (DATABASE_CONNECTION_STRING) is not configured.");
        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public")
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IInspectionLock, PostgresInspectionLock>();

        // ---- Identity ----
        services.AddIdentityCore<User>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IAccessCodeHasher, AccessCodeHasher>();
        services.AddSingleton<IClock, SystemClock>();

        // ---- Cross-cutting ----
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<INotificationService, DatabaseNotificationService>();

        // ---- Storage ----
        services.AddSingleton<UrlSigner>();
        services.AddSingleton<IStorageService>(sp =>
        {
            var provider = sp.GetRequiredService<IOptions<StorageOptions>>().Value.Provider;
            return provider.ToUpperInvariant() switch
            {
                "LOCAL" => ActivatorUtilities.CreateInstance<LocalFileStorageService>(sp),
                _ => throw new InvalidOperationException($"Storage provider '{provider}' is not supported yet (see docs/architecture/0006-storage-abstraction.md)."),
            };
        });

        // ---- AI ----
        services.AddHttpClient<OpenAiImageAnalysisService>();
        services.AddSingleton<MockImageAnalysisService>();
        services.AddScoped<IImageAnalysisService>(sp => ResolveAnalyzer(sp, env));
        services.AddSingleton<ChannelAiAnalysisQueue>();
        var inline = configuration.GetValue("Ai:ProcessInline", false);
        if (inline)
        {
            services.AddSingleton<IAiAnalysisQueue, InlineAiAnalysisQueue>();
        }
        else
        {
            services.AddSingleton<IAiAnalysisQueue>(sp => sp.GetRequiredService<ChannelAiAnalysisQueue>());
            services.AddHostedService<AiAnalysisWorker>();
        }

        // ---- Billing ----
        services.AddSingleton<SandboxPaymentProvider>();
        services.AddHttpClient<StripePaymentProvider>();
        services.AddScoped<IPaymentProvider>(sp => PaymentProviderSelector.Resolve(sp, env));
        services.AddScoped<SandboxCheckoutSimulator>();

        // ---- PDF ----
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        services.AddSingleton<IPdfService, QuestPdfReportService>();

        // ---- Background ----
        if (configuration.GetValue("Background:ExpiryWorkerEnabled", true))
            services.AddHostedService<InspectionExpiryWorker>();

        // ---- Module application services ----
        services.AddScoped<AuthService>();
        services.AddScoped<CompanyAccess>();
        services.AddScoped<CompanyService>();
        services.AddScoped<PropertyService>();
        services.AddScoped<TenancyService>();
        services.AddScoped<InspectionAccess>();
        services.AddScoped<InspectionWriteScope>();
        services.AddScoped<RoomContextLoader>();
        services.AddScoped<InspectionDetailsBuilder>();
        services.AddScoped<InspectionService>();
        services.AddScoped<AgentInspectionService>();
        services.AddScoped<InvitationService>();
        services.AddScoped<InspectionMaintenanceService>();
        services.AddScoped<MediaUrlService>();
        services.AddScoped<MediaService>();
        services.AddScoped<AiAnalysisService>();
        services.AddScoped<AiAnalysisProcessor>();
        services.AddScoped<ReportSnapshotStore>();
        services.AddScoped<ReportSnapshotBuilder>();
        services.AddScoped<ReportImageLoader>();
        services.AddScoped<FinalizationService>();
        services.AddScoped<ReportService>();
        services.AddScoped<TenantService>();
        services.AddScoped<SubscriptionAccessService>();
        services.AddScoped<CheckoutService>();
        services.AddScoped<BillingWebhookProcessor>();
        return services;
    }

    private static IImageAnalysisService ResolveAnalyzer(IServiceProvider sp, IHostEnvironment env)
    {
        var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        var hasKey = !string.IsNullOrWhiteSpace(options.OpenAI.ApiKey);
        var provider = options.Provider.ToUpperInvariant();
        var useOpenAi = provider == "OPENAI" || (provider == "AUTO" && hasKey);
        if (useOpenAi) return sp.GetRequiredService<OpenAiImageAnalysisService>();

        if (!env.IsDevelopment() && !options.AllowMockOutsideDevelopment && !env.IsEnvironment("Testing"))
        {
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("AI")
                .LogError("No OpenAI API key configured and mock AI is not allowed in {Env}.", env.EnvironmentName);
            return new UnavailableImageAnalysisService();
        }
        return sp.GetRequiredService<MockImageAnalysisService>();
    }
}

/// <summary>Picks the payment provider, mirroring the AI selection: a real provider when configured, the Sandbox only where allowed.</summary>
internal static class PaymentProviderSelector
{
    public static IPaymentProvider Resolve(IServiceProvider sp, IHostEnvironment env)
    {
        var provider = sp.GetRequiredService<IOptions<BillingOptions>>().Value.Provider.ToUpperInvariant();
        var hasStripeKey = !string.IsNullOrWhiteSpace(sp.GetRequiredService<IOptions<StripeOptions>>().Value.SecretKey);
        if (provider == "STRIPE" || (provider == "AUTO" && hasStripeKey)) return sp.GetRequiredService<StripePaymentProvider>();

        var sandboxAllowed = env.IsDevelopment() || env.IsEnvironment("Testing") || sp.GetRequiredService<IOptions<SandboxOptions>>().Value.AllowOutsideDevelopment;
        if (sandboxAllowed) return sp.GetRequiredService<SandboxPaymentProvider>();
        sp.GetRequiredService<ILoggerFactory>().CreateLogger("Billing")
            .LogError("No payment provider configured and the sandbox is not allowed in {Env}.", env.EnvironmentName);
        return new UnavailablePaymentProvider();
    }
}

/// <summary>Used outside Development when AI is not configured: analyses fail with a clear, recoverable message.</summary>
internal sealed class UnavailableImageAnalysisService : IImageAnalysisService
{
    public string ProviderName => "unavailable";
    public string? Model => null;
    public bool IsMock => false;
    private static AiProviderException Error() => new("AI descriptions are not configured on this server. Please write the description manually.");
    public Task<RoomAnalysisResult> AnalyzeRoomAsync(RoomAnalysisRequest request, CancellationToken cancellationToken = default) => throw Error();
    public Task<DefectAnalysisResult> AnalyzeDefectAsync(DefectAnalysisRequest request, CancellationToken cancellationToken = default) => throw Error();
    public Task<ComparisonAnalysisResult> CompareRoomAsync(RoomComparisonRequest request, CancellationToken cancellationToken = default) => throw Error();
}
