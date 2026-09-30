# Implementation report — InspectFlow MVP

## 1. Starting point

The repository was empty (no existing stack, code, tests or conventions), so the preferred stack from the brief was used as-is: Next.js + React + TypeScript, ASP.NET Core Web API on .NET 10, EF Core, PostgreSQL, Docker Compose, QuestPDF, ASP.NET Core Identity + JWT + refresh tokens, xUnit.

## 2. Summary

A working, persistent, tested MVP covering the three roles end to end:

- **Company**: sign-up → workspace → properties with configurable rooms (optionally creating the tenancy and first inspection in the same form) → tenancies + tenant invitations → Move In / Move Out / Periodic / Other inspections, public (marketplace) or private (link + hashed 6-digit code) → progress tracking → finalized reports, share links, send-to-tenant.
- **Agent**: marketplace with privacy-preserving previews → race-safe accept → mobile-first two-step execution — a single photos page for every room and defect (camera upload with progress/retry, client-side resize), then a descriptions page where all AI texts, requested at once, arrive via background processing and polling (autosave, defects with classification/confirmation) → Move In baseline view and comparison decision for Move Out → review page (exact report preview, editable text) → finalize.
- **Tenant**: invitation acceptance → dashboard (awaiting / accepted / disputed) → report review, room-level and general observations → accept or dispute.
- **Public entry & billing**: landing page (hero product mockup, audiences, how it works, features, pricing) → company registration → plan → checkout at the payment provider (Stripe, or the Sandbox in development) → activation only from signed, idempotent webhooks → dashboard. Company API access requires an active subscription, enforced by an authorization requirement (402 `subscription.required`), and abandoned checkouts are resumed after signing in again (ADR 0012).
- **Reports**: immutable versioned JSON snapshot + QuestPDF PDF (SHA-256 of both), web report page, expiring share links, Move Out comparison summary.

Validation performed: backend and frontend build without warnings; ESLint clean; 141 backend tests pass (unit + HTTP/PostgreSQL integration); migrations applied to empty databases (tests and compose); `docker compose up` stack verified; the Playwright UI script runs the full Move In + Move Out scenario across three browser sessions (desktop company/tenant, mobile agent) against the Docker stack, including PDF download.

## 3. Architecture

Modular monolith — `Shared`, `Modules` (Identity, Companies, Properties, Tenancies, Inspections, Media, AI, Reports, Tenants, Notifications, Audit; each with `Domain/` and `Application/`), `Infrastructure`, `Api`. One PostgreSQL database with a schema per module. Rationale and trade-offs in `docs/architecture/0001…0010`.

Abstractions keeping the domain vendor-free: `IImageAnalysisService` (OpenAI / mock / unavailable), `IStorageService` (local signed URLs; S3/Blob-ready incl. direct upload hook), `IPdfService` (QuestPDF), `INotificationService` (in-app store + log), `IAuditLogger`, `ITokenService`, `IAccessCodeHasher`, `IInspectionLock`, `IAiAnalysisQueue`.

## 4. Main files

| Area | Files |
|---|---|
| Domain | `backend/src/Modules/*/Domain/*.cs` — notably `Inspections/Domain/{Inspection,InspectionRoom,InspectionDefect,InspectionComparison,InspectionInvitation,InspectionStateMachine,InspectionReadiness}.cs`, `Properties/Domain/Property.cs`, `Tenancies/Domain/Tenancy.cs`, `Reports/Domain/InspectionReport.cs` |
| Use cases | `Modules/Identity/Application/AuthService.cs`, `Companies/Application/{CompanyAccess,CompanyService}.cs`, `Properties/Application/PropertyService.cs`, `Tenancies/Application/TenancyService.cs`, `Inspections/Application/{InspectionService,AgentInspectionService,InvitationService,InspectionAccess,InspectionWriteScope,RoomContextLoader,InspectionDetailsBuilder,BasicComparisonBuilder}.cs`, `Media/Application/MediaService.cs`, `AI/Application/{AiAnalysisService,AiAnalysisProcessor,AiPrompts,IImageAnalysisService}.cs`, `Reports/Application/{FinalizationService,ReportService,ReportSnapshotBuilder,ReportSnapshot,ReportSnapshotStore}.cs`, `Tenants/Application/TenantService.cs` |
| Infrastructure | `Infrastructure/Persistence/{AppDbContext,ModelConfiguration,PostgresInspectionLock}.cs`, `Migrations/*`, `AI/{OpenAiImageAnalysisService,MockImageAnalysisService,AnalysisSchemas,AiQueue}.cs`, `Storage/{LocalFileStorageService,UrlSigner}.cs`, `Pdf/QuestPdfReportService.cs`, `Identity/*`, `Seed/DatabaseInitializer.cs`, `DependencyInjection.cs` |
| API | `backend/src/Api/Program.cs`, `Endpoints/{Auth,Company,Agent,Tenant,Shared}Endpoints.cs`, `Infrastructure/{ErrorHandling,RateLimits,EnvironmentAliases}.cs` |
| Frontend | `frontend/src/lib/{api,auth,hooks,autosave,image,types}.ts*`, `components/*`, `app/**/page.tsx` |
| Tests | `backend/tests/InspectFlow.Tests/{Domain,Integration,Support}/*`, `e2e/ui-flow.mjs` |
| Ops/docs | `docker-compose.yml`, `backend/Dockerfile`, `frontend/Dockerfile`, `.env.example`, `README.md`, `docs/architecture/*` |

## 5. Migrations

- `InitialCreate` — all tables, schemas (`identity, companies, properties, tenancies, inspections, media, ai, reports, tenants, notifications, audit`), indexes, FKs, xmin concurrency tokens.
- `AddBilling` — schema `billing`: `subscriptions` (filtered unique index: one open subscription per user), `checkout_sessions` (one Open session per subscription), `billing_events` (unique provider event id).
- `AppendOnlyTriggers` — PostgreSQL triggers rejecting UPDATE/DELETE on report versions, audit logs, tenant responses and observations.

## 6. Pages

Public: Landing, Pricing, Checkout, Sandbox payment, Subscription success, Subscription required, `/app` entry. Login, Register (with a company plan step), Company: Onboarding, Dashboard, Properties, Create Property, Property Details (rooms, tenancies, invitations, inspections), Create Inspection, Inspection Details. Agent: Dashboard, Available Inspections, Available Inspection details/accept, Private invitation (code entry), My Inspections, Completed Inspections, Inspection Execution, Room Inspection, Inspection Review. Tenant: Dashboard, Report Review, Invitation. Reports: authenticated Report Page, shared (token) Report Page.

## 7. Decisions taken during implementation

- Modules as folders inside one `Modules` assembly (plus architecture tests) instead of one project per module — avoids circular project references between Inspections/Media/AI/Reports while keeping boundaries explicit; can be split later.
- Minimal APIs; application services throw typed exceptions mapped to ProblemDetails with stable codes.
- Company scope is resolved from `CompanyMember` server-side; the MVP gives each company user one workspace (Owner) with role permissions prepared.
- Tenants join tenancies by single-use invitation links and must sign in with the invited email.
- Private invitation: token (SHA-256) + code (PBKDF2), 7-day expiry, 5 attempts, single agent; the code is not sent with the link.
- Finalization automatically sends the report to tenants when the tenancy has members; otherwise the company sends it later.
- AI runs in the background with durable state in `ai_analyses`; tests use inline processing.
- AI text pre-fills the agent text only when empty (conditional SQL update); AI never confirms defects or decides comparisons.
- Baseline for Move Out is read from the Move In **report snapshot**, not live rows; baselines must be from the same tenancy.
- Rooms are archived (not deleted) so original ids keep matching across inspections.
- A completed room automatically reopens if an edit breaks a completion rule; review/finalize re-validate everything anyway.
- Web auth: refresh token only in an HttpOnly SameSite=Strict cookie through the same-origin Next.js proxy; native apps get it in the body.
- Development seed goes through the real application services (so seeded data satisfies every rule) and never runs outside Development.
- Development secrets in compose are prefixed `dev-only-` and rejected outside Development.

## 8. Independent review

A separate review pass over authorization, immutability, secrets and concurrency found no authorization holes; seven lower-severity issues were fixed with regression tests where applicable (AI result could update live rows of a just-finalized inspection → now applied under the inspection lock; tokenised invitation links no longer persisted/logged in notifications; baseline tenancy check extended to Periodic; tenant emails redacted for agents; tenant observation vs. decision race → locked; completed rooms reopen on breaking edits; photo file deleted only after commit).

## 9. Definition of Done

All items are met: backend and frontend compile; database is created from scratch by migrations; authentication, roles, company/agent/tenant flows, properties & rooms, tenancies, public and private inspections, upload, AI (OpenAI implementation + labelled mock fallback), agent review, Move In, Move Out, basic comparison (+ AI-assisted comparison), finalization, PDF, tenant review work; authorization is tested; critical tests pass; Docker environment works; README documents setup.

Caveats to be transparent about:
- The **real OpenAI call was not exercised** (no API key available here). The adapter is covered by contract tests with a fake HTTP handler (request shape, strict schema, safety prompt, error mapping, key never in body/errors). Validate once with a real key; the default model name (`gpt-4.1-mini`) is configurable via `OPENAI_MODEL`.
- Docker images were built here with an extra CA secret because this sandbox intercepts TLS; on a normal machine `docker compose up --build` needs nothing extra.

### AI writing guide (2026-09-30)

AI drafts follow a team-maintained, versioned writing guide (`backend/src/Modules/AI/Guidelines/inspection-writing-guide.md`) appended to every analysis after the safety rules. They are written in the company's report language (`en` / `pt-BR`), and `prompt_version` records code, guide and language (ADR 0013).

## 10. Known limitations, technical debt and future work

See README → *Known limitations & technical debt* and *Deliberately out of scope*. Most relevant next steps:
1. Put a reverse proxy in front that forwards client IPs (per-IP rate limits currently see the web server's IP when traffic goes through the Next.js rewrite) — or move sign-in limits per account.
2. S3/Azure Blob storage + direct signed-URL uploads (hooks exist: `CreateDirectUploadAsync`, `MediaStatus.Pending`).
3. Durable queue for AI jobs when running more than one API instance; background PDF generation for very large reports.
4. Email delivery for notifications (`TransientLink` is the hook), email verification, password reset, MFA.
5. Company staff management UI (roles/permissions already modelled), dispute-resolution workflow and report re-issuing (versioning in place), per-inspection room selection.
