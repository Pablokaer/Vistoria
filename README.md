# InspectFlow — property inspection platform (MVP)

InspectFlow connects **companies** (letting agents / property managers), **inspectors** (agents) and **tenants**:

- Companies register properties with any number of rooms, manage tenancies and publish inspections (Move In, Move Out, Periodic, Other) — publicly in a marketplace or privately via link + 6-digit code.
- Inspectors accept an inspection, go room by room on their phone, upload photos, get AI-drafted descriptions (clearly labelled, always editable), record defects, compare with the Move In baseline, review and finalize.
- Finalization freezes an immutable, versioned report (JSON snapshot + PDF, both hashed).
- Tenants review the report, add observations (general or per room) and accept or dispute it.

The web app is the first client; the API is designed so iOS/Android apps can use it unchanged (JWT + refresh token in body for native clients).

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS 4 |
| Backend | ASP.NET Core Web API on .NET 10 (minimal APIs), EF Core 10, Npgsql |
| Database | PostgreSQL 16 (one schema per module) |
| Auth | ASP.NET Core Identity, JWT access tokens, rotating refresh tokens |
| AI | OpenAI Chat Completions (vision + strict JSON-schema output) behind `IImageAnalysisService`; labelled mock provider for development |
| PDF | QuestPDF (Community license — free for companies under USD 1M annual revenue; review the [QuestPDF license](https://www.questpdf.com/license/) for your case) |
| Storage | `IStorageService` — local disk with signed URLs (S3 / Azure Blob pluggable) |
| Tests | xUnit, WebApplicationFactory, Testcontainers (PostgreSQL); Playwright UI flow |
| Infra | Docker, Docker Compose |

## Architecture

Modular monolith (see [`docs/architecture`](docs/architecture/README.md) for the decisions and their reasons).

```
backend/
  src/
    Shared/          kernel: errors, clock, ICurrentUser, secure tokens
    Modules/         business modules — each with Domain/ (pure C#) and Application/ (use cases, DTOs, abstractions)
      Identity/  Companies/  Properties/  Tenancies/  Inspections/  Media/
      AI/  Reports/  Tenants/  Notifications/  Audit/  Common/ (IAppDbContext, options)
    Infrastructure/  EF Core DbContext + migrations, Identity/JWT, local storage, OpenAI + mock AI,
                     QuestPDF renderer, background workers, development seed
    Api/             HTTP endpoints per module, auth policies, rate limiting, ProblemDetails
  tests/InspectFlow.Tests/
    Domain/          state machine, room rules, comparison, architecture, OpenAI adapter
    Integration/     real HTTP + PostgreSQL: authorization, flows, concurrency, immutability
frontend/            Next.js app (Company, Agent and Tenant areas)
e2e/                 Playwright script driving the full Move In + Move Out flow through the UI
docs/architecture/   ADRs
```

Key rules live in the domain: the inspection **state machine** (`InspectionStateMachine`), room completion rules (`InspectionRoom.GetCompletionIssues`), readiness for review/finalization (`InspectionReadiness`). The API only maps HTTP to application services.

```
Draft → Open → Assigned → InProgress ⇄ Review → Completed → AwaitingTenant → Accepted | Disputed
Draft/Open/Assigned/InProgress → Cancelled        Open → Expired (acceptance deadline passed)
```

### Main entities

| Module | Entities |
|---|---|
| Identity | `User`, `Role`, `UserRole`, `RefreshToken`, `AgentProfile` |
| Companies | `Company`, `CompanyMember` (roles Owner/Admin/PropertyManager/Employee/Viewer, permissions prepared) |
| Properties | `Property`, `PropertyRoom` |
| Tenancies | `Tenancy`, `TenancyMember` |
| Inspections | `Inspection`, `InspectionRoom` (snapshot), `InspectionDefect`, `InspectionComparison`, `InspectionInvitation` |
| Media | `InspectionRoomMedia` (metadata only; binaries in storage) |
| AI | `AiAnalysis` (provider, model, prompt version, photos, structured result) |
| Reports | `InspectionReport`, `InspectionReportVersion` (immutable), `ReportShareLink` |
| Tenants | `TenantProfile`, `TenantObservation`, `TenantResponse` |
| Notifications / Audit | `Notification`, `AuditLog` |

## Running locally

### Option A — Docker Compose (recommended)

Prerequisites: Docker with Compose v2.

```bash
cp .env.example .env        # optional for local use: compose has development defaults
docker compose up --build
```

| Service | URL |
|---|---|
| Web app | http://localhost:3000 |
| API | http://localhost:8080 (OpenAPI document: `/openapi/v1.json` in Development) |
| PostgreSQL | localhost:5432 (`inspectflow` / `inspectflow`) |

On start-up the API applies migrations and, in `Development`, seeds demo data. Data persists in the `pgdata` and `storage` volumes; `docker compose down -v` resets everything.

> Behind a corporate TLS-inspecting proxy, pass your CA to the builds:
> `docker build --secret id=extra_ca,src=/path/ca.crt -t inspectflow-api ./backend` (same for `./frontend` → `inspectflow-web`), then `docker compose up --no-build`.

### Option B — run the processes yourself

Prerequisites: .NET SDK 10, Node.js 22, PostgreSQL 16 (e.g. `docker compose up db`).

```bash
# API (http://localhost:8080)
cd backend
export ASPNETCORE_ENVIRONMENT=Development
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=inspectflow;Username=inspectflow;Password=inspectflow"
export JWT_SECRET="dev-only-jwt-secret-change-me-0123456789abcdef"
export STORAGE_SIGNING_KEY="dev-only-storage-signing-key-0123456789abcdef"
dotnet run --project src/Api        # migrates + seeds in Development

# Web (http://localhost:3000)
cd frontend
npm install
npm run dev                          # proxies /api to API_INTERNAL_URL (default http://localhost:8080)
```

### Database migrations

Migrations live in `backend/src/Infrastructure/Persistence/Migrations` (`InitialCreate`, `AppendOnlyTriggers`). They run automatically when `MIGRATE_ON_STARTUP=true` (default in Development/compose). Manually:

```bash
dotnet tool install -g dotnet-ef
cd backend
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=inspectflow;Username=inspectflow;Password=inspectflow"
dotnet ef database update -p src/Infrastructure -s src/Infrastructure            # apply to an empty database
dotnet ef migrations add <Name> -p src/Infrastructure -s src/Infrastructure -o Persistence/Migrations
```

For production, prefer a reviewed SQL script (`dotnet ef migrations script --idempotent ...`) or a migration bundle over migrating on start-up.

## Configuration

All settings can be given as the flat environment variables below (see `.env.example`) or as standard ASP.NET Core keys (`Jwt__Secret`, `Ai__OpenAI__Model`, …).

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` enables demo seed + OpenAPI + mock AI. Use `Production` otherwise. |
| `DATABASE_CONNECTION_STRING` | Npgsql connection string |
| `JWT_SECRET` | ≥ 32 chars HMAC key for access tokens (`dev-only-*` values are rejected outside Development) |
| `STORAGE_SIGNING_KEY` | ≥ 32 chars HMAC key for signed file URLs |
| `STORAGE_PROVIDER`, `STORAGE_LOCAL_PATH`, `STORAGE_PUBLIC_BASE_URL` | Storage backend (Local), path, optional absolute URL prefix for native clients |
| `AI_PROVIDER` | `Auto` (default), `OpenAI` or `Mock` |
| `OPENAI_API_KEY`, `OPENAI_MODEL`, `OPENAI_BASE_URL` | OpenAI settings (server-side only) |
| `WEB_BASE_URL` | Base URL used in invitation and share links |
| `MIGRATE_ON_STARTUP`, `SEED_DEMO_DATA` | Start-up behaviour (seed is ignored outside Development) |
| `TRUST_FORWARDED_HEADERS` | Honour `X-Forwarded-For` (only when the API is reachable exclusively via a trusted proxy) |
| `CORS_ALLOWED_ORIGINS` | Comma-separated origins for browser clients on other origins (not needed for the bundled web app) |
| `API_INTERNAL_URL` (frontend, build time) | Where Next.js proxies `/api/*` |

Business limits (`Inspections__MinimumGeneralPhotosPerRoom`, `MaxPhotosPerRoom`, `MaxUploadBytes` (15 MB), invitation and share-link lifetimes) and rate limits (`RateLimiting__AuthPerMinute`, …) are in `backend/src/Api/appsettings.json`.

### OpenAI

1. Set `OPENAI_API_KEY` (environment variable, `.env`, `dotnet user-secrets` or your secret store — never commit it).
2. Optionally set `OPENAI_MODEL` (default `gpt-4.1-mini`; any vision model supporting structured outputs).
3. Restart the API. `GET /api/ai/status` shows the active provider (the key is never returned).

The key is used only by the API process, only in the `Authorization` header of outbound requests; it is never sent to the browser, stored in the database or written to logs/audit metadata. Prompts are in `backend/src/Modules/AI/Application/AiPrompts.cs`.

### Mock AI mode

Without a key (or with `AI_PROVIDER=Mock`) in Development, `MockImageAnalysisService` returns deterministic, plausible structured results. Every text starts with **"[Development mock AI — photos were not actually analysed]"** and the UI shows a "Development mock AI" badge. Outside Development the mock is disabled unless `Ai__AllowMockOutsideDevelopment=true`; AI requests then fail with a clear message and inspectors write descriptions manually.

## Demo users (Development only)

Seeded automatically when the environment is `Development` and the database is empty. Password for all: **`Demo@12345`**.

| Role | Email | Seeded data |
|---|---|---|
| Company | `company@demo.local` | "Demo Property Management"; **12 Main Street** (6 rooms, tenancy, **open public Move In**); **48 Oak Avenue** (6 rooms, tenancy, **finalized & accepted Move In** with photos, a defect, report + PDF — ready for a Move Out) |
| Agent | `agent@demo.local` | "Demo Inspector" (did the 48 Oak Avenue Move In) |
| Tenant | `tenant@demo.local` | "Demo Tenant", member of both tenancies |

Demo credentials are never created outside Development.

## How to test the flows

**Company** — sign in as `company@demo.local` (or register a Company account → create workspace). Dashboard → *Add property* (rooms editor) → property page: edit rooms, create a tenancy, invite a tenant (copy the invitation link) → *New inspection* → choose type/tenancy/visibility → publish. Private inspections show the link and 6-digit code **once**.

**Agent** — sign in as `agent@demo.local` (best on a phone or a narrow browser window). *Available* → open → *Accept inspection* → *Start inspection* → each room: *Take photo / Upload photos* → *Generate AI description* → edit the text (autosaved; drafts survive a refresh) → tick *Defects found* → *Add defect* (label, photos, AI description, classification, *I confirm this defect*) → *Mark room complete*. When all rooms are complete: *Review inspection* → edit texts if needed → *Finalize inspection* → report page with *Download PDF*.
Private inspection: open the invitation link while signed in as an agent and enter the code (5 attempts, rate limited).

**Tenant** — open the invitation link from the company (register with the invited email, *Accept invitation*), or sign in as `tenant@demo.local`. *My inspections* → open a report awaiting review → *Add observation about this room* / *I want to add observations* → *Everything is correct* or *I disagree* (reason required).

**Move In** — as above on 12 Main Street (seeded open inspection) or any new property with a tenancy.

**Move Out** — as the company: *New inspection* for **48 Oak Avenue** → type *Move Out* → the finalized Move In is pre-selected under *Compare with* → publish. As an agent: accept/start; every room shows the read-only **Move In record** (text, photos, defects). Add photos and defects, use *Compare recorded data* (and optionally *AI visual comparison*), pick your decision (Unchanged, Normal wear, New damage, …) and save it — a room can't be completed without it. Finalize: the report and PDF contain the comparison summary. The tenant then reviews it.

## Tests

```bash
cd backend
dotnet test                                   # needs Docker (Testcontainers starts PostgreSQL)
# or against an existing server (a temporary database is created and dropped):
TEST_DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=postgres;Username=inspectflow;Password=inspectflow" dotnet test

cd frontend && npm run lint && npm run build

# Full UI flow (Move In + Move Out, 3 roles) against a running stack; screenshots in e2e/output
cd e2e && npm install && BASE_URL=http://localhost:3000 node ui-flow.mjs
```

86 backend tests cover: authorization across companies/roles/tenants, property & rooms, room snapshot, publish rules, public accept (+ 8 parallel agents → exactly one wins), private link + code (hashing, attempt lock, single use, regeneration), state transitions, upload validation (content sniffing, size, ownership, after-finalization), room completion and blocked finalization, finalization + PDF, report immutability (hash, EF guard, DB trigger), share links, tenant access/observations/accept/dispute (+ concurrent decisions), completed rooms reopening when an edit breaks a rule, same-tenancy baselines, the full Move In and Move Out flows through HTTP, the OpenAI adapter contract (strict schema, safety prompt, no key leakage) and architecture boundaries.

## API overview

| Area | Endpoints (prefix `/api`) |
|---|---|
| Auth | `POST auth/register`, `auth/login`, `auth/refresh`, `auth/logout`, `GET auth/me` |
| Company | `POST companies`, `GET company/dashboard`, `GET/POST properties`, `GET/PUT properties/{id}`, `POST/PUT/DELETE properties/{id}/rooms[/{roomId}]`, `PUT properties/{id}/rooms/order`, `GET properties/{id}/tenancies`, `POST tenancies`, `POST tenancies/{id}/tenants`, `GET/POST inspections`, `GET/PUT inspections/{id}`, `POST inspections/{id}/publish|cancel|invitation|send-to-tenant`, `POST reports/{id}/share-links` |
| Agent | `GET agent/dashboard`, `GET agent/available[/{id}]`, `GET/POST agent/invitations/{token}[/verify]`, `POST agent/inspections/{id}/accept|start|submit-review|return-to-progress|finalize`, `GET agent/inspections/{id}/review`, `GET/PUT agent/inspections/{id}/rooms/{roomId}`, `POST .../photos|analysis|complete|reopen|defects|comparison/basic|comparison/analysis`, `PUT .../comparison/decision`, `PUT/DELETE .../defects/{defectId}` |
| Tenant | `GET tenant/dashboard`, `GET tenant/invitations/{token}`, `POST tenant/invitations/{token}/accept`, `GET tenant/inspections/{id}/report`, `POST tenant/inspections/{id}/observations|accept|dispute` |
| Shared | `GET inspections/{id}/report`, `GET reports/{id}`, `GET shared/reports/{token}`, `GET files/{key}?exp&sig`, `GET ai/status`, `GET notifications`, `GET /health` |

Errors are RFC 7807 problem details with a stable `code` (e.g. `inspection.invalid_transition`, `room.incomplete` with `details`).

## Security notes

- Authorization is enforced in application services (membership/assignment checks on every query); the UI only hides what the API already forbids.
- Private access codes: PBKDF2 hashes; tokens (invitations, share links, refresh tokens): SHA-256 of 256-bit random values; plaintext shown once.
- Rate limits on sign-in/registration, invitation verification, public endpoints, uploads and AI; Identity lockout after 5 failed sign-ins.
- Uploads validated by magic bytes (JPEG/PNG/WebP), size and ownership; stored under server-generated keys; served via expiring HMAC-signed URLs with `nosniff`.
- `Referrer-Policy: no-referrer` (tokens appear in invitation paths), `X-Frame-Options: DENY`.
- Audit log (`audit.audit_logs`, append-only) records the key events; metadata containing secret-like keys is refused.

## Known limitations & technical debt

- **Rate limiting behind the web proxy**: the bundled Next.js rewrite does not forward the client IP, so per-IP limits apply to the web server's IP. In production route `/api` through a reverse proxy that sets `X-Forwarded-For` and enable `TRUST_FORWARDED_HEADERS`, or move to per-account limits. Identity lockout still protects accounts. The Next.js proxy also has a 30 s timeout per request.
- **AI queue is in-process**: durable in the database and re-queued at start-up, but with several API instances use a real queue/worker.
- **Local storage only**; S3/Azure Blob implementations and direct-to-bucket uploads are prepared but not written. Photos are not resized server-side (the web client downsizes before upload; the PDF uses 144 DPI rasterization).
- **Notifications** are stored in-app and logged; no email/push delivery yet (invitation links are shown to the company to share).
- No email verification, password reset, MFA, or company staff management UI (roles/permissions exist in the model).
- Multiple tenants: the first decision (accept/dispute) closes the review. No dispute-resolution workflow or report re-issuing yet (versioning supports it).
- Rooms cannot be excluded per inspection (all active rooms are snapshotted; `IsRequired` prepared).
- PDF generation runs synchronously during finalization (≈1–2 s for typical reports); very large reports should move to a background job.
- Accessibility and i18n are basic (English UI, en-IE formats).
- EF logs one `fail` line on a brand-new database (migrations history table lookup) — harmless.

## Deliberately out of scope for this MVP

Native mobile apps, payments/subscriptions, agent ratings, real-time chat, own ML / fine-tuning, microservices, Kubernetes, distributed queues, mandatory geolocation, legal e-signatures, automatic liability decisions, integrations with external agencies.
