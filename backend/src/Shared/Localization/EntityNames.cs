namespace InspectFlow.Shared.Localization;

/// <summary>
/// Entity names used in "not found" messages, so every module names things the same way in both languages.
/// Example: <c>throw new NotFoundException(EntityNames.Room, roomId);</c>
/// </summary>
public static class EntityNames
{
    public static readonly LocalizedText Analysis = new("Analysis", "Análise");
    public static readonly LocalizedText Checkout = new("Checkout", "Pagamento");
    public static readonly LocalizedText SandboxCheckout = new("Sandbox checkout", "Pagamento de teste");
    public static readonly LocalizedText Defect = new("Defect", "Avaria");
    public static readonly LocalizedText Inspection = new("Inspection", "Vistoria");
    public static readonly LocalizedText Invitation = new("Invitation", "Convite");
    public static readonly LocalizedText PaymentProvider = new("Payment provider", "Provedor de pagamento");
    public static readonly LocalizedText Photo = new("Photo", "Foto");
    public static readonly LocalizedText Property = new("Property", "Imóvel");
    public static readonly LocalizedText Report = new("Report", "Laudo");
    public static readonly LocalizedText Room = new("Room", "Cômodo");
    public static readonly LocalizedText ShareLink = new("Share link", "Link de compartilhamento");
    public static readonly LocalizedText Tenancy = new("Tenancy", "Locação");
}
