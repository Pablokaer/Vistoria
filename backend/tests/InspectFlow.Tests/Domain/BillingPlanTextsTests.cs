using InspectFlow.Modules.Billing.Application;

namespace InspectFlow.Tests.Domain;

public class BillingPlanTextsTests
{
    private static BillingPlan Plan(BillingPlanTexts? portuguese = null)
    {
        var plan = new BillingPlan { Code = "pro", Name = "Professional", Description = "All you need", Features = ["Reports"] };
        if (portuguese is not null) plan.Translations["pt-BR"] = portuguese;
        return plan;
    }

    [Fact]
    public void TextsIn_returns_the_translation_matching_the_tag_case_insensitively()
    {
        var texts = Plan(new BillingPlanTexts { Name = "Profissional", Description = "Tudo", Features = ["Laudos"] }).TextsIn("PT-br");
        Assert.Equal(("Profissional", "Tudo", "Laudos"), (texts.Name, texts.Description, texts.Features.Single()));
    }

    [Fact]
    public void TextsIn_falls_back_to_English_per_empty_field_and_for_unknown_languages()
    {
        var partial = Plan(new BillingPlanTexts { Name = "Profissional" }).TextsIn("pt-BR");
        Assert.Equal(("Profissional", "All you need", "Reports"), (partial.Name, partial.Description, partial.Features.Single()));
        Assert.Equal("Professional", Plan().TextsIn("pt-BR").Name);
    }
}
