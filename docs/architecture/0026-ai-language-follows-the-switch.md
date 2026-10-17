# 0026 — AI drafts and room names follow the PT | EN switch

**Context.** ADR 0013 wrote every AI text in the company's report language. An inspector who set the app to Portuguese still got English drafts from an English company, so a Portuguese screen showed English text. Room names had the same problem. They are stored as typed, so the seeded rooms and every property created with the English UI ("Living Room", "Bedroom 1") stayed in English after the switch to PT.

**Decision.**

- **AI language = the requester's language.** When an inspector requests a description (room, defect or comparison), the API resolves the request language from `Accept-Language`, which the web app sets from the PT | EN switch (`CurrentLanguage`). It stores that language on the analysis (`ai.ai_analyses.output_language`, `en` / `pt-BR`), and `prompt_version` records it as before. With no supported language, it falls back to English.
- **The background worker reads the stored language.** The worker that calls the model runs outside the request, so it cannot see the switch. It no longer reads the company setting.
- **Migration.** Earlier analyses are backfilled with their company's report language, which is the language they were written in.
- **Default room names are shown in the UI language.** On the frontend, `localizeRoomName` (exposed as `useFormatters().roomName`) translates a room name only when it is a room-type label in either language, optionally followed by a number. For example, "Bedroom 2" ↔ "Quarto 2" and "Living Room" ↔ "Sala de estar". Any other name ("Suíte master") is the company's own text and is shown as typed. "Other" is not translated. Stored names never change. This applies to every screen that lists rooms: capture and descriptions pages, inspection overviews, the company's property and progress panels, and the web report.

**Consequences.**

- The report language (*Settings*) still decides the PDF labels and the report texts the system writes. When an inspector works in a different language, the AI drafts they accept are in their language, inside a report whose labels are in the company's language. The inspector reviews every draft before finalizing.
- Requesting again after switching language starts a new analysis in the new language. A request that is still running is reused, whatever its language.
- The PDF shows room names as stored. Translating them there is left for later.
