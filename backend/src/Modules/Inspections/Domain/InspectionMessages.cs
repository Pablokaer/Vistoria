using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>User-facing texts thrown from more than one place in the Inspections module (kept here so they stay identical).</summary>
public static class InspectionMessages
{
    public static readonly LocalizedText NotAvailable = new(
        "This inspection has already been accepted or is not available.",
        "Esta vistoria já foi aceita ou não está disponível.");

    public static readonly LocalizedText TooManyIncorrectCodes = new(
        "Too many incorrect codes. Ask the company for a new invitation.",
        "Muitos códigos incorretos. Peça um novo convite à empresa.");

    public static readonly LocalizedText NoBaseline = new(
        "This room has no baseline inspection to compare with.",
        "Este cômodo não tem uma vistoria de referência para comparação.");

    public static readonly LocalizedText ChangedConcurrently = new(
        "The inspection was changed by someone else. Refresh and try again.",
        "A vistoria foi alterada por outra pessoa. Atualize a página e tente novamente.");
}
