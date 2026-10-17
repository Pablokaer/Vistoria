# 0013 — AI writing guide as versioned prompt content; output in the company's report language

**Context.** AI drafts must read photos the way an experienced inventory clerk does: systematic, specific, with
consistent vocabulary. The quality bar should be improvable by the team over time without code changes. Drafts were
always in British English, but the reports belong to companies that may work in another language.

**Decision.**
- The team-maintained guide `backend/src/Modules/AI/Guidelines/inspection-writing-guide.md` is embedded in the
  Modules assembly and appended to every analysis's system message. It covers:
  - reading order;
  - condition criteria;
  - materials;
  - room, defect and comparison structure;
  - room checklists;
  - style;
  - before/after examples;
  - a revision log.
- Only the text below the `=== GUIDE FOR THE MODEL ===` line goes to the model. The header tells the team how to grow
  the guide.
- System message order: the safety rules (`AiPrompts.SafetyRules`, code-owned and non-negotiable), then the language
  rule, then the guide. The guide cannot override the safety rules.
- The guide's first line `guide-version: YYYY-MM-DD.n` is bumped with each edit. Every analysis stores
  `prompt_version = <code version>+g<guide version>+<language>`, for example `2026-09-v2+g2026-09-30.1+pt-BR`, so
  quality can be compared before and after an edit and per language.
- **Output language** is the company's `ReportLanguage` (`SupportedLanguages`: `en`, `pt-BR`), mapped by
  `AiOutputLanguage` to the name given to the model ("British English", "Brazilian Portuguese").
  - It is company-wide, not per user, because a finalized report is frozen and must not depend on the inspector who
    wrote it.
  - Free text follows the language. JSON keys, condition codes and classification codes stay in English, so parsing
    and UI mapping keep working.
- The guide itself is written in English (models follow English instructions most reliably). The language rule tells
  the model to copy the examples' level of detail, not their language.

**Consequences.**
- Improving drafts is a Markdown edit plus a version bump, reviewed like code. The build's tests reject a guide
  without its version line or marker.
- The guide is sent with every call, adding roughly 2–3k input tokens per analysis. Keep it concise and replace weak
  rules instead of piling up new ones.
- The development mock cannot translate: its label names the language a real provider would use.
- The language is read when the analysis runs. A draft requested seconds before the company changes its language may
  come out in the previous language; `prompt_version` records the language that was actually used.

## Update 2026-10-17 — report voice (prompt 2026-10-v3, guide 2026-10-17.1)

**Context.** Drafts read like descriptions of pictures ("No visible damage is apparent in the provided images", "The
photo shows…"), walls or floor were sometimes skipped, and a close-up of a defect among the room photos was not
treated as a defect. Brazilian Portuguese drafts had no vistoria terminology to follow.

**Decision.**
- The free text is written **as the inspector's own report entry**: impersonal, present tense, never referring to AI or
  to "the image". Photos are mentioned only in `limitations`. This rule lives in the code-owned `SafetyRules`, whose
  example phrase changed to inventory wording ("No visible damage noted.").
- `RoomInstruction`: an opening sentence about the room as a whole (type, windows and light, furnished or not); walls
  and floor are always covered (or stated as not visible); a defect close-up among the room photos goes into
  `observedDefects` and is mentioned in the paragraph.
- `DefectInstruction`: what it is, the surface it is on, where, approximate size, how it looks. It starts from the
  inspector's label, describes only what the photos show, and never invents a position a close-up does not show.
- The guide fixes the paragraph order (room → ceiling → walls → floor → windows/doors → fittings → furniture →
  defects → cleanliness → not assessed), lists phrases that give away a machine-written text, and adds Brazilian
  Portuguese terminology and examples.
- The development mock writes in the same voice and order.

**Consequences.**
- `ReportVoiceTests` pin what reaches the model (room and defect rules, inspector voice, pt-BR terminology), the
  guide's 2,500-word budget and the mock's voice.
- Not measured against the real provider here: no OpenAI key was available. Compare drafts by `prompt_version`
  (`2026-10-v3+g2026-10-17.1+…` against `2026-09-v2+g2026-09-30.1+…`) once real inspections run.

**Update 2026-10-17 (language).** AI texts no longer follow the company's report language: they follow the PT / EN switch of the inspector who requests them, stored on each analysis. See [ADR 0026](0026-ai-language-follows-the-switch.md).
