using InspectFlow.Shared.Localization;

namespace InspectFlow.Infrastructure.Pdf;

/// <summary>The PDF label sets. The English set reproduces the texts the PDF used before localization.</summary>
internal static class ReportPdfTexts
{
    public static readonly ReportPdfText English = new()
    {
        Language = SupportedLanguages.English,
        MonthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
        LongDateFormat = "{0} {1} {2}",
        ShortMonthSuffix = "",
        ReportTitleFormat = "{0} Inspection Report",
        DocumentTitleFormat = "Inspection report {0}",
        DocumentSubjectFormat = "{0} inspection - {1}",
        ReportNumberFormat = "Report {0}",
        VersionFormat = "Version {0}",
        GeneratedFormat = "Generated {0} UTC",
        FooterReportIdFormat = "{0} · Report ID {1}",
        Page = "Page ",
        PageOf = " of ",
        Property = "Property",
        PropertyType = "Property type",
        InspectionType = "Inspection type",
        InspectionDate = "Inspection date",
        Inspector = "Inspector",
        Tenants = "Tenant(s)",
        Tenancy = "Tenancy",
        Ongoing = "ongoing",
        ComparedWith = "Compared with",
        RoomsInspected = "Rooms inspected",
        Disclaimer = "This report records the condition of the property that was visible at the time of the inspection. " +
                     "Descriptions may have been drafted with AI assistance and were reviewed and confirmed by the inspector. " +
                     "Areas not shown or not accessible were not assessed. This report does not determine responsibility or liability for any damage.",
        ComparisonSummaryTitle = "Move In / Move Out comparison summary",
        ComparisonSummaryFormat = "Compared with report {0} · {1} room(s) · {2}",
        Room = "Room",
        InspectorDecision = "Inspector decision",
        Notes = "Notes",
        Condition = "Condition: ",
        NoDescription = "No description recorded.",
        DefectsFormat = "Defects ({0})",
        Defect = "Defect",
        NoDefects = "No defects recorded.",
        InspectorNotes = "Inspector notes: ",
        ComparisonWithMoveIn = "Comparison with Move In",
        MoveInRecord = "Move In record: ",
        MoveInDefects = "Move In defects: ",
        PhotoUnavailable = "Photo unavailable",
        EnumLabels = new Dictionary<string, string>
        {
            ["MoveIn"] = "Move In", ["MoveOut"] = "Move Out", ["LivingRoom"] = "Living Room", ["DiningRoom"] = "Dining Room",
            ["NewDamage"] = "New damage", ["PreExisting"] = "Pre-existing", ["NormalWear"] = "Normal wear",
            ["UnableToDetermine"] = "Unable to determine",
        },
    };

    public static readonly ReportPdfText PortugueseBrazil = new()
    {
        Language = SupportedLanguages.PortugueseBrazil,
        MonthNames = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"],
        LongDateFormat = "{0} de {1} de {2}",
        ShortMonthSuffix = ".",
        ReportTitleFormat = "Laudo de vistoria — {0}",
        DocumentTitleFormat = "Laudo de vistoria {0}",
        DocumentSubjectFormat = "Vistoria de {0} - {1}",
        ReportNumberFormat = "Laudo {0}",
        VersionFormat = "Versão {0}",
        GeneratedFormat = "Gerado em {0} UTC",
        FooterReportIdFormat = "{0} · ID do laudo {1}",
        Page = "Página ",
        PageOf = " de ",
        Property = "Imóvel",
        PropertyType = "Tipo de imóvel",
        InspectionType = "Tipo de vistoria",
        InspectionDate = "Data da vistoria",
        Inspector = "Vistoriador",
        Tenants = "Inquilino(s)",
        Tenancy = "Locação",
        Ongoing = "em vigor",
        ComparedWith = "Comparado com",
        RoomsInspected = "Cômodos vistoriados",
        Disclaimer = "Este laudo registra o estado do imóvel que estava visível no momento da vistoria. " +
                     "As descrições podem ter sido redigidas com apoio de IA e foram revisadas e confirmadas pelo vistoriador. " +
                     "Áreas não mostradas ou sem acesso não foram avaliadas. Este laudo não determina responsabilidade por nenhum dano.",
        ComparisonSummaryTitle = "Resumo da comparação entre entrada e saída",
        ComparisonSummaryFormat = "Comparado com o laudo {0} · {1} cômodo(s) · {2}",
        Room = "Cômodo",
        InspectorDecision = "Decisão do vistoriador",
        Notes = "Observações",
        Condition = "Estado: ",
        NoDescription = "Nenhuma descrição registrada.",
        DefectsFormat = "Avarias ({0})",
        Defect = "Avaria",
        NoDefects = "Nenhuma avaria registrada.",
        InspectorNotes = "Observações do vistoriador: ",
        ComparisonWithMoveIn = "Comparação com a vistoria de entrada",
        MoveInRecord = "Registro da entrada: ",
        MoveInDefects = "Avarias na entrada: ",
        PhotoUnavailable = "Foto indisponível",
        EnumLabels = new Dictionary<string, string>
        {
            ["MoveIn"] = "Entrada", ["MoveOut"] = "Saída", ["Periodic"] = "Periódica", ["Other"] = "Outra",
            ["House"] = "Casa", ["Apartment"] = "Apartamento", ["Studio"] = "Studio", ["Townhouse"] = "Sobrado",
            ["Bungalow"] = "Casa térrea", ["Commercial"] = "Comercial",
            ["LivingRoom"] = "Sala de estar", ["Bedroom"] = "Quarto", ["Kitchen"] = "Cozinha", ["Bathroom"] = "Banheiro",
            ["DiningRoom"] = "Sala de jantar", ["Hallway"] = "Corredor", ["Garage"] = "Garagem", ["Garden"] = "Jardim",
            ["Utility"] = "Área de serviço", ["Office"] = "Escritório", ["Balcony"] = "Varanda",
            ["NewDamage"] = "Dano novo", ["PreExisting"] = "Pré-existente", ["NormalWear"] = "Desgaste natural",
            ["Resolved"] = "Resolvido", ["Unchanged"] = "Sem alteração", ["UnableToDetermine"] = "Não foi possível determinar",
            ["Unknown"] = "Não classificada", ["Pending"] = "Pendente",
        },
    };
}
