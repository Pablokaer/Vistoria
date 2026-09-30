# 0014 — Two languages (English, Brazilian Portuguese): UI per user, reports per company

**Context.** The platform has to work in both English and Portuguese. Several kinds of text are involved, and they do not all belong to the same person: the screens belong to whoever is reading them, while a report is a legal-ish record that the company, the inspector and the tenant all read. That record is frozen and hashed at finalization (ADR 0007), and AI drafts become part of it (ADR 0013).

**Decision.**
- **Languages:** `en` and `pt-BR`. Any other Portuguese tag (`pt`, `pt-PT`) maps to `pt-BR`; anything else falls back to English. Backend `SupportedLanguages` and frontend `src/i18n/locales.ts` hold the same list.
- **UI language is per user and per browser.** The `locale` cookie, set by the header switcher, wins; without it the browser's `Accept-Language` decides. The language is not part of the URL. The app has no public content that needs indexing per language, and URLs stay stable for invitation and share links. The root layout resolves the locale on the server, so the first render is already in the right language. This makes pages dynamic, which is an acceptable cost for this app.
- **Frontend texts** live in small catalogs per area (`src/i18n/messages/*.ts`) declared with `defineMessages(en, ptBR)`. The Portuguese object must have exactly the English keys, so a missing translation fails `tsc`. There is no i18n library: `translator`, `useT` and `getServerTranslator` are about 40 lines. Dates, money and enum labels come from `createFormatters(locale)`.
- **API messages follow the request.** The web app sends `Accept-Language` with every call.
  - `UseRequestLanguage()` runs before the exception handler. It resolves the header by q-value (`RequestLanguage.FromAcceptLanguage`) and sets `CurrentLanguage`, an `AsyncLocal`.
  - ASP.NET's `RequestLocalization`/`CultureInfo` is deliberately not used. The app runs with `InvariantGlobalization`, so there is no pt-BR culture, and only the texts should follow the reader, not number or date formatting.
  - Exceptions carry a `LocalizedText` (every language at once). The exception handler reads the language from the request itself (`RequestLanguage.Of`). The string-only exception constructors were removed, so an untranslated message fails the build.
  - `Exception.Message` stays English, so logs stay in one language.
  - Stable `code` values are unchanged, so clients can still branch on them.
  - Plan names, descriptions and features come from configuration, with optional `Translations.{tag}` per plan.
- **Reports and AI text follow the company.** `Company.ReportLanguage` (`en` or `pt-BR`) is set when the company is created (the owner's UI language) and can be changed in *Settings* by an Owner or Admin. AI drafts are written in it (ADR 0013). The report snapshot records it (`language`, schema v2; v1 snapshots read as English), and the PDF labels use it (`ReportPdfText`, with its own month names because no pt-BR culture exists at runtime). A finalized report keeps its language even if the company later switches.
- **User-typed content is never translated** (room names, descriptions, observations).

**Consequences.**
- On the web, a Portuguese-speaking tenant can read an English report with Portuguese screen labels around it. The PDF is always entirely in the report language.
- Adding a language means adding it to both lists, adding one more object to every catalog, adding one more `LocalizedText` field, and adding a PDF label set. The `LocalizedText(En, PtBr)` shape is deliberately simple for two languages. A third language would justify moving to resource files.
- Emails and notifications do not exist yet. When they are added, they need a stored per-user language, because a background job has no request culture.
