import { defineMessages } from "../translate";

// Inspection cards and their adapters (components/inspection/*).
export const inspectionListMessages = defineMessages(
  {
    withBaseline: "With Move In baseline",
    rooms: "{count} rooms",
    scheduled: "Scheduled {date}",
    acceptBy: "Accept by {date}",
    updated: "Updated {date}",
    completedOn: "Completed {date}",
    noAgentYet: "No inspector yet",
    ctaView: "View inspection",
    ctaStart: "Start inspection",
    ctaContinue: "Continue inspection",
    ctaReview: "Review and finalize",
    ctaReport: "View report",
    ctaOpen: "Open",
  },
  {
    withBaseline: "Com base da vistoria de entrada",
    rooms: "{count} cômodos",
    scheduled: "Agendada para {date}",
    acceptBy: "Aceitar até {date}",
    updated: "Atualizada em {date}",
    completedOn: "Concluída em {date}",
    noAgentYet: "Sem vistoriador ainda",
    ctaView: "Ver vistoria",
    ctaStart: "Iniciar vistoria",
    ctaContinue: "Continuar vistoria",
    ctaReview: "Revisar e finalizar",
    ctaReport: "Ver laudo",
    ctaOpen: "Abrir",
  },
);

export type InspectionListKey = keyof (typeof inspectionListMessages)["en"];
