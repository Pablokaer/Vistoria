using InspectFlow.Shared.Localization;

namespace InspectFlow.Modules.Inspections.Domain;

/// <summary>
/// Human names for inspection enums inside error messages, so a Portuguese message does not show "MoveIn" or
/// "AwaitingTenant". Example: <c>InspectionTerms.Of(InspectionStatus.Review).PtBr // "em revisão"</c>.
/// </summary>
public static class InspectionTerms
{
    private static readonly Dictionary<InspectionStatus, LocalizedText> Statuses = new()
    {
        [InspectionStatus.Draft] = new("Draft", "rascunho"),
        [InspectionStatus.Open] = new("Open", "aberta"),
        [InspectionStatus.Assigned] = new("Assigned", "atribuída"),
        [InspectionStatus.InProgress] = new("InProgress", "em andamento"),
        [InspectionStatus.Review] = new("Review", "em revisão"),
        [InspectionStatus.Completed] = new("Completed", "concluída"),
        [InspectionStatus.AwaitingTenant] = new("AwaitingTenant", "aguardando inquilino"),
        [InspectionStatus.Accepted] = new("Accepted", "aceita"),
        [InspectionStatus.Disputed] = new("Disputed", "contestada"),
        [InspectionStatus.Cancelled] = new("Cancelled", "cancelada"),
        [InspectionStatus.Expired] = new("Expired", "expirada"),
    };

    private static readonly Dictionary<InspectionType, LocalizedText> Types = new()
    {
        [InspectionType.MoveIn] = new("MoveIn", "de entrada"),
        [InspectionType.MoveOut] = new("MoveOut", "de saída"),
        [InspectionType.Periodic] = new("Periodic", "periódica"),
        [InspectionType.Other] = new("Other", "de outro tipo"),
    };

    // English keeps the enum identifier: existing clients and tests match on it (e.g. "cannot move from Draft to Completed").
    public static LocalizedText Of(InspectionStatus status) => Statuses[status];

    public static LocalizedText Of(InspectionType type) => Types[type];
}
