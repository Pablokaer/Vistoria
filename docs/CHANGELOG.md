# Changelog

Every code change is recorded here (newest first), together with the docs it touched.

## 2026-09-30

- **PDF report redesign.** See [ADR 0016](architecture/0016-pdf-report-layout.md).
  - **Structure:**
    - `PdfTheme` holds the tokens and `PdfIcons` the line icons and logo.
    - `Components/` has one component each for the cover, the rooms index with page links, the page chrome, the room, the photo grid, the defect, the Move In × Move Out comparison and the summary.
    - `QuestPdfReportService` only assembles the document.
  - **Page breaks:**
    - A room header stays with its photos.
    - Condition text stays with the last photo row.
    - A single photo sits beside a short condition.
    - A defect description sits beside its photos.
    - "Possible changes" and the inspector's decision form one block, so the decision is never orphaned on the next page.
    - The summary, including report details, stays on one page.
  - **Fix:** Move Out reports now load the Move In photos (`ReportImageLoader.PhotoKeys` includes `Comparison.BaselinePhotos`). Before, the Before column showed "Photo unavailable".
  - **Removed:** the unused `ReportPdfText.Classification` label.
  - **Tests:**
    - `ReportPdfRenderingTests` renders seven scenarios from `Support/ReportSamples`: no defects, defects, many photos, long texts, optional data missing, Move Out en and pt-BR.
    - A regression test checks that the Move In photos are loaded.
    - With `PDF_PREVIEW_DIR` set, the tests write each PDF and its page PNGs for review.
- **Product UI redesign (frontend only; no endpoint, rule or state changed).** See [ADR 0015](architecture/0015-design-system.md).
  - **Design system:** tokens in `app/globals.css` (type scale, neutrals, brand blue, semantic tones, Move In / Move Out / Periodic identities, radii, shadows, widths, motion), the Inter font through `next/font`, and inline icons (`components/icons.tsx`). The primitives in `components/ui/*` replace `components/ui.tsx`: StatusBadge with icon and label, MetricCard, EmptyState, Skeleton, Tabs, ProgressRing, Switch, toasts, and `useConfirm` instead of `window.confirm`.
  - **Shell:** a navy sidebar per role with notifications (titles translated from the notification type), profile, language and sign-out. On phones, a top bar, a drawer and bottom tabs; the tabs are hidden during inspection execution. The dev account switcher is now a discreet chip.
  - **Inspector:**
    - The dashboard shows the current inspection with a Continue button, compact metrics, a marketplace preview and recently completed inspections.
    - The marketplace gets search, filters and sorting (client-side).
    - *My inspections* is a single tabbed view.
    - A new inspection details page with a next-step panel.
    - Execution:
      - A step header on every page.
      - Room navigation: side list on desktop, chip strip on phones.
      - A capture area with a large *Take photo*, a viewer and retry.
      - AI draft states with "Restore".
      - Defects that expand when needed.
      - A Before/After comparison.
      - Sticky action bars.
  - **Company:** a pipeline dashboard, a property table, a sectioned property page and forms, and an inspection timeline built from the real timestamps.
  - **Tenant:** a simple dashboard (awaiting review first) and a response panel.
  - **Reports:** a document layout with a photo lightbox and a side panel (contents, review, versions, integrity), plus *Share report* for companies.
  - **Sign-in, sign-up and checkout:** aligned with the new look.
  - **Fix found during the pass:** the new next-step panel called `/agent/…/start` without `/api`, which returned 404. It now calls `/api/agent/…`.
  - **Tests:** the e2e now clicks the in-app confirmations (*Yes, finalize*, *Yes, the report is correct*). `tsc`, `eslint`, the Docker production build and the full UI flow pass.

- **English and Brazilian Portuguese across the platform.** See [ADR 0014](architecture/0014-localization.md).
  - **Web:** every page and component has both languages, with catalogs in `frontend/src/i18n/messages/*.ts` built on a small dependency-free i18n core (`src/i18n`). The first language comes from the browser; the **EN / PT** switcher in the app header, the landing header, sign-in/register and checkout changes it and stores the choice in the `locale` cookie. Server components render in the right language on the first byte. Dates, money and enum labels are localized, and `lib/format.ts` was removed.
  - **Company Settings page** (`/company/settings`) with the **report language**. Onboarding sends the owner's current language as the company's report language.
  - **API:** every user-facing message (about 170: validation, domain rules, room completion issues, not-found, 402, 429, Identity and AI errors) is a `LocalizedText`, and the string-only exception constructors were removed. The language comes from `Accept-Language` through `UseRequestLanguage` (`AsyncLocal`, because globalization is invariant). Codes are unchanged, and English is still the default.
  - **Plans:** optional `Translations.pt-BR` in `Billing:Plans`.
  - **Reports:** the snapshot records `language` (schema v2; v1 reads as English), and the PDF's labels and dates follow it (`ReportPdfText`).
  - **Tests:** 37 new backend tests (178 in total). The e2e flow adds `e2e/localization.mjs`: a Portuguese browser, the switcher and cookie, and a Portuguese API error.

- **Company report language (`en` / `pt-BR`).** `Company.ReportLanguage` (migration `AddCompanyReportLanguage`, default `en`) sets the language of the company's reports and AI drafts. `POST /api/companies` accepts an optional `reportLanguage`; `PUT /api/companies/me/report-language` changes it (Owner/Admin, audited); `/api/auth/me` and `GET /api/companies/me` return it. New `Shared/Localization`: `SupportedLanguages` (tag resolution/validation) and `LocalizedText` (one text in every supported language), the base for the bilingual UI and API messages. Tests: `LocalizationTests`, `CompanyReportLanguageTests`.
- **AI writing guide and report language for AI drafts.** New team-maintained guide `backend/src/Modules/AI/Guidelines/inspection-writing-guide.md` (reading order, condition criteria, materials, room/defect/comparison structure, room checklists, style, before/after examples, revision log). It is embedded and sent with every analysis after the safety rules, so improving AI drafts is a Markdown edit plus a `guide-version` bump. AI free text is now written in the company's report language (`en` → British English, `pt-BR` → Brazilian Portuguese); JSON keys and condition/classification codes stay in English. `prompt_version` records code, guide and language (e.g. `2026-09-v2+g2026-09-30.1+pt-BR`). The mock provider names the language in its label. See [ADR 0013](architecture/0013-ai-writing-guide-and-language.md). Tests: `WritingGuideTests` (guide parsing, prompt order, language rule, OpenAI system message, prompt version).
- **`run-project.sh` is executable in git** (mode 100755), so `./run-project.sh` works straight after cloning on Linux/macOS without `chmod +x`.
- **Public landing page and mandatory subscription.** `/` is now a public landing page with a hero product mockup, the audiences, how it works, highlights, features, pricing and a final CTA; its header has a mobile menu, Sign in and Get started. New pages: `/pricing`, `/checkout` (plan + order summary), `/checkout/sandbox/[sessionId]` (development payment page), `/subscription/success` (polls until the webhook activates the subscription), `/subscription/required` (unpaid, abandoned or lapsed accounts) and `/app` (sends each user to their home). `/register?plan=…` is the company step of the flow. Backend: new `Billing` module (`Subscription`, `CheckoutSession`, `BillingEvent`; migration `AddBilling`), `IPaymentProvider` with Stripe and Sandbox implementations, signed and idempotent webhooks at `POST /api/billing/webhooks/{provider}`, and an `ActiveSubscriptionRequirement` on the role policies (402 `subscription.required`). Roles that pay are configured in `Billing:SubscriptionRequiredRoles` (default: Company). `/api/auth/me` includes a `subscription` summary. The demo company is seeded with an active subscription. The brand colour moves from teal to blue. See [ADR 0012](architecture/0012-subscription-billing.md). Tests: 30 new backend tests (116 in total), and the UI flow now covers landing → subscribe and abandoned checkout → sign in → resume → dashboard.
- **Add property + inspection in one form.** *Add property* has an **Also create an inspection for this property** checkbox. Ticking it enables the inspection form (Move In / Periodic / Other, tenancy start/end date, visibility, dates, instructions, publish now); one button creates the property, the tenancy (if a start date is given; required for Move In) and the inspection. A failed inspection step keeps the property and retries only the inspection. The shared inspection fields live in `frontend/src/components/InspectionForm.tsx`, also used by *New inspection*.
- **Dev account switcher (Development only).** Header dropdown **Dev: switch account** signs in as the seeded company, agent or tenant. Backed by `GET /api/dev/accounts` and `POST /api/dev/switch`, mapped only when `ASPNETCORE_ENVIRONMENT=Development` (404 elsewhere, covered by a test).
- **Two-step agent flow.** One photos page for every room and its defects, then one descriptions page; all AI texts are requested in a single batch when leaving the photos page. See [ADR 0011](architecture/0011-capture-then-describe.md). The per-room page was removed.
- **`run-project.sh`** launcher: `up` (default), `stop`, `reset`, `logs`.
- Initial import of the MVP.
