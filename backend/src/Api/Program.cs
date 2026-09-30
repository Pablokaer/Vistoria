using System.Text;
using System.Text.Json.Serialization;
using InspectFlow.Api.Endpoints;
using InspectFlow.Api.Infrastructure;
using InspectFlow.Infrastructure;
using InspectFlow.Infrastructure.Identity;
using InspectFlow.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(EnvironmentAliases.FromEnvironment());

builder.Services.AddInspectFlow(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret.PadRight(32))),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name",
        RoleClaimType = "role",
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.Company, p => p.RequireAuthenticatedUser().RequireRole(Policies.Company))
    .AddPolicy(Policies.Agent, p => p.RequireAuthenticatedUser().RequireRole(Policies.Agent))
    .AddPolicy(Policies.Tenant, p => p.RequireAuthenticatedUser().RequireRole(Policies.Tenant));

var origins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins.Length > 0) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Only enable when the API is reachable exclusively through a trusted proxy (see README).
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });
}

var app = builder.Build();

if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", false)) app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h.XContentTypeOptions = "nosniff";
    h.XFrameOptions = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevEndpoints();
}

app.MapAuthEndpoints();
app.MapCompanyEndpoints();
app.MapAgentEndpoints();
app.MapTenantEndpoints();
app.MapSharedEndpoints();

if (!app.Configuration.GetValue("Database:SkipInitialization", false))
    await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

public partial class Program;
