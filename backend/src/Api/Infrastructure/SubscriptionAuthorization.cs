using System.IdentityModel.Tokens.Jwt;
using InspectFlow.Modules.Billing.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using InspectFlow.Shared.Localization;
using Microsoft.AspNetCore.Mvc;

namespace InspectFlow.Api.Infrastructure;

/// <summary>
/// Added to every role policy. Succeeds immediately for roles that do not need a subscription
/// (Billing:SubscriptionRequiredRoles); otherwise the subscription is read from the database on each request.
/// </summary>
public sealed class ActiveSubscriptionRequirement : IAuthorizationRequirement
{
    public const string FailureCode = "subscription.required";
}

public sealed class ActiveSubscriptionHandler(SubscriptionAccessService access) : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveSubscriptionRequirement requirement)
    {
        var roles = context.User.FindAll("role").Select(c => c.Value);
        if (!access.IsRequiredFor(roles))
        {
            context.Succeed(requirement);
            return;
        }
        if (!Guid.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)) return;
        var result = await access.GetAccessAsync(userId, CancellationToken.None);
        if (result.HasAccess) context.Succeed(requirement);
        else context.Fail(new AuthorizationFailureReason(this, ActiveSubscriptionRequirement.FailureCode));
    }
}

/// <summary>
/// Turns a failure caused only by the subscription requirement into 402 Payment Required with
/// code "subscription.required", so clients can send the user to the subscription page instead of an error.
/// </summary>
public sealed class SubscriptionAwareResultHandler(IProblemDetailsService problems) : IAuthorizationMiddlewareResultHandler
{
    private static readonly LocalizedText SubscriptionRequired =
        new("An active subscription is required.", "É necessária uma assinatura ativa.");

    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!IsSubscriptionFailure(authorizeResult))
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }
        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status402PaymentRequired,
            Title = SubscriptionRequired.In(RequestLanguage.Of(context)),
            Type = $"https://inspectflow.dev/errors/{ActiveSubscriptionRequirement.FailureCode}",
        };
        details.Extensions["code"] = ActiveSubscriptionRequirement.FailureCode;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details });
    }

    private static bool IsSubscriptionFailure(PolicyAuthorizationResult result) =>
        result.Forbidden && result.AuthorizationFailure is { } failure &&
        failure.FailedRequirements.All(r => r is ActiveSubscriptionRequirement) &&
        failure.FailureReasons.Any(r => r.Message == ActiveSubscriptionRequirement.FailureCode);
}
