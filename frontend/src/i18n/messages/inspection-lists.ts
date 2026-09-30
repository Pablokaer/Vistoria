import { defineMessages } from "../translate";

// Inspection cards shared by the agent area (components/InspectionLists.tsx).
export const inspectionListMessages = defineMessages(
  {
    withBaseline: "With Move In baseline",
    rooms: "{count} rooms",
    scheduled: "Scheduled {date}",
    acceptBy: "Accept by {date}",
  },
  {
    withBaseline: "Com base da vistoria de entrada",
    rooms: "{count} cômodos",
    scheduled: "Agendada para {date}",
    acceptBy: "Aceitar até {date}",
  },
);
