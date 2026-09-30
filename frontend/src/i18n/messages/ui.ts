import { defineMessages } from "../translate";

// Built-in texts of the shared UI primitives (components/ui.tsx).
export const uiMessages = defineMessages(
  {
    loading: "Loading…",
    retry: "Retry",
    copy: "Copy",
    copied: "Copied",
    roomsCompleted: "{value} / {total} rooms completed",
  },
  {
    loading: "Carregando…",
    retry: "Tentar novamente",
    copy: "Copiar",
    copied: "Copiado",
    roomsCompleted: "{value} / {total} cômodos concluídos",
  },
);
