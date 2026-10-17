# Olhai — property inspection platform (MVP)

> **Name.** The product is called **Olhai**: that is the name on the logo, page titles, texts and the PDF report. `InspectFlow` remains the internal code name (C# namespaces, database, containers) — see [ADR 0020](docs/architecture/0020-product-name-olhai.md).

Olhai connects **companies** (letting agents / property managers), **inspectors** (agents) and **tenants**:

- A public **landing page** (`/`) presents the product with architectural photography, a phone mockup of the real inspection screen, the four-step process, a section about working with your own inspectors (*Inspector sign-in*) and the plans from the API ([ADR 0019](docs/architecture/0019-landing-page-redesign.md)); signed-in users who open `/` go straight to their dashboard ([ADR 0018](docs/architecture/0018-landing-page-and-root-routing.md)). There is **no free plan**: *Get started* → create a company account → choose the plan → pay → the subscription is activated by the payment provider's confirmation → dashboard. A company that abandons checkout can sign in later and continue from *Subscription required*, without registering again. Inspector accounts (created by the company) and tenant accounts are free.

- Companies register properties with any number of rooms, manage tenancies and publish inspections (Move In, Move Out, Periodic, Other) to **their own agents**: **Public** (every agent of the company sees it under *Available*; the first to accept takes it) or **Direct** (sent to one agent when it is created; only that agent sees it). A property and its first inspection can be created together from one form.
- **Agents belong to their company.** Inspectors cannot sign up themselves: the company creates their accounts on the *Agents* page (an email already used by any account is refused). An agent only ever sees its company's Public inspections and the ones sent directly to it; agents of other companies get 404. Until the agent starts, the company can send an inspection to another agent or release it back to the whole team, also when an agent accepted it from the list ([ADR 0017](docs/architecture/0017-company-team-agents.md), [ADR 0022](docs/architecture/0022-agents-belong-to-their-company.md)). There is no public marketplace and no private link + code.
- Inspectors accept an inspection and work in two steps on their phone: first they photograph every room and its defects on a single page, then all AI-drafted descriptions (clearly labelled, always editable) are requested at once and reviewed on a second page; Move Outs are compared with the Move In baseline; then review and finalize.
- Finalization freezes an immutable, versioned report (JSON snapshot + PDF, both hashed).
- Tenants review the report, add observations (general or per room) and accept or dispute it.
- **English and Brazilian Portuguese.** Every screen, API message and report exists in both. The UI follows the browser language until the user picks one: the **PT | EN switch** at the bottom of the sidebar (drawer on phones) inside the app, or the EN / PT selector on public pages; AI descriptions follow that switch at the moment they are requested, and default room names ("Bedroom 1" ↔ "Quarto 1") are shown in it too. Reports and PDF labels follow the company's **report language** (*Settings*).
- **Product UI.** A role-aware sidebar (drawer + bottom tabs on phones), dashboards organised around the next action (inspector: current inspection, preview of the company's open inspections, recently completed; company: pipeline, recent inspections and reports; tenant: reports awaiting review), a searchable/filterable *Available* list, tabbed "My inspections", a phone-first inspection flow (room navigation, big camera button, AI drafts clearly marked and editable, progressive defects, Before/After comparison) and document-style reports with a separate action toolbar. Built on a small in-house design system (`frontend/src/app/globals.css` tokens + `components/ui`, see [ADR 0015](docs/architecture/0015-design-system.md)).

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
| Billing | `IPaymentProvider`: Stripe Checkout (REST, signed webhooks) or a labelled Sandbox provider for development |
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
      AI/  Reports/  Tenants/  Notifications/  Audit/  Billing/  Common/ (IAppDbContext, options)
    Infrastructure/  EF Core DbContext + migrations, Identity/JWT, local storage, OpenAI + mock AI,
                     QuestPDF renderer, background workers, development seed
    Api/             HTTP endpoints per module, auth policies, rate limiting, ProblemDetails
  tests/InspectFlow.Tests/
    Domain/          state machine, room rules, comparison, architecture, OpenAI adapter
    Integration/     real HTTP + PostgreSQL: authorization, flows, concurrency, immutability
frontend/            Next.js app (Company, Agent and Tenant areas; texts per language in src/i18n;
                     design tokens in app/globals.css, primitives in components/ui, app shell in components/shell)
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
| Companies | `Company`, `CompanyMember` (roles Owner/Admin/PropertyManager/Employee/Viewer, permissions prepared), `CompanyAgent` (the company's team of agents) |
| Properties | `Property`, `PropertyRoom` |
| Tenancies | `Tenancy`, `TenancyMember` |
| Inspections | `Inspection` (visibility `Public` / `Direct`), `InspectionRoom` (snapshot), `InspectionDefect`, `InspectionComparison` |
| Media | `InspectionRoomMedia` (metadata only; binaries in storage) |
| AI | `AiAnalysis` (provider, model, prompt version, photos, structured result) |
| Reports | `InspectionReport`, `InspectionReportVersion` (immutable), `ReportShareLink` |
| Tenants | `TenantProfile`, `TenantObservation`, `TenantResponse` |
| Notifications / Audit | `Notification`, `AuditLog` |
| Billing | `Subscription` (Pending/Active/PastDue/Cancelled/Expired), `CheckoutSession`, `BillingEvent` |

## Running locally

### Option A — Docker Compose (recommended)

Prerequisites: Docker with Compose v2.

```bash
cp .env.example .env        # optional for local use: compose has development defaults
docker compose up --build
```

Or use the launcher script (Git Bash / WSL on Windows), which checks Docker, creates `.env` from `.env.example` if missing, builds, waits until the web app answers and prints the URLs:

```bash
./run-project.sh          # build + start, wait until ready
./run-project.sh stop     # stop containers (data kept)
./run-project.sh reset    # stop and delete all data (database + uploaded files)
./run-project.sh logs     # follow logs
```

| Service | URL |
|---|---|
| Web app | http://localhost:3000 |
| API | http://localhost:8080 (OpenAPI document: `/openapi/v1.json` in Development) |
| PostgreSQL | localhost:15432 (`inspectflow` / `inspectflow`; `POSTGRES_PORT` changes it — not 5432, so a locally installed PostgreSQL never clashes) |

On start-up the API applies migrations and, in `Development`, seeds demo data. Data persists in the `pgdata` and `storage` volumes; `docker compose down -v` resets everything.

All three ports are published on `127.0.0.1` only, so the demo users, the dev account switcher and the default database password are not reachable from the network. To try the web app from a phone on the same Wi-Fi, set `WEB_BIND_ADDRESS=0.0.0.0` in `.env` and open `http://<this machine's LAN IP>:3000`. This file is for development only; servers use `docker-compose.prod.yml` (see *Production deployment*).

> Behind a corporate TLS-inspecting proxy, pass your CA to the builds:
> `docker build --secret id=extra_ca,src=/path/ca.crt -t inspectflow-api ./backend` (same for `./frontend` → `inspectflow-web`), then `docker compose up --no-build`.

### Option B — run the processes yourself

Prerequisites: .NET SDK 10, Node.js 22, PostgreSQL 16 (e.g. `docker compose up db`).

```bash
# API (http://localhost:8080)
cd backend
export ASPNETCORE_ENVIRONMENT=Development
export DATABASE_CONNECTION_STRING="Host=localhost;Port=15432;Database=inspectflow;Username=inspectflow;Password=inspectflow"
export JWT_SECRET="dev-only-jwt-secret-change-me-0123456789abcdef"
export STORAGE_SIGNING_KEY="dev-only-storage-signing-key-0123456789abcdef"
dotnet run --project src/Api        # migrates + seeds in Development

# Web (http://localhost:3000)
cd frontend
npm install
npm run dev                          # proxies /api to API_INTERNAL_URL (default http://localhost:8080)
```

### Database migrations

Migrations live in `backend/src/Infrastructure/Persistence/Migrations` (`InitialCreate`, `AppendOnlyTriggers`, `AddBilling`, `AddCompanyReportLanguage`, `AddCompanyAgents`). They run automatically when `MIGRATE_ON_STARTUP=true` (default in Development/compose). Manually:

```bash
dotnet tool install -g dotnet-ef
cd backend
export DATABASE_CONNECTION_STRING="Host=localhost;Port=15432;Database=inspectflow;Username=inspectflow;Password=inspectflow"
dotnet ef database update -p src/Infrastructure -s src/Infrastructure            # apply to an empty database
dotnet ef migrations add <Name> -p src/Infrastructure -s src/Infrastructure -o Persistence/Migrations
```

The production compose file migrates on start-up too (one API instance; see [ADR 0024](docs/architecture/0024-production-deployment-and-trusted-proxies.md)). To review and apply the SQL yourself instead, generate it with `dotnet ef migrations script --idempotent -p src/Infrastructure -s src/Infrastructure -o migrate.sql`, apply it with `psql`, and set `MIGRATE_ON_STARTUP=false`.

### Production deployment

`docker-compose.prod.yml` is a separate, standalone file. It runs `ASPNETCORE_ENVIRONMENT=Production` with no demo seed, requires every secret, and publishes nothing but a Caddy reverse proxy on ports 80/443. Caddy routes `/api/*` and `/health` straight to the API and everything else to the web app; database, API and web app are reachable only on the internal network.

```bash
cp .env.production.example .env.production   # fill in every value; the file is gitignored
docker compose -f docker-compose.prod.yml --env-file .env.production up --build -d
docker compose -f docker-compose.prod.yml --env-file .env.production ps
```

- **Address:** set `SITE_ADDRESS` to your domain and point its DNS at the server, with ports 80 and 443 open. Caddy then obtains and renews the HTTPS certificate itself. Behind another TLS terminator, use `SITE_ADDRESS=:80`. `WEB_BASE_URL` is the public URL used in links, e.g. `https://app.example.com`.
- **Secrets:** compose refuses to start while `POSTGRES_PASSWORD`, `JWT_SECRET`, `STORAGE_SIGNING_KEY`, `WEB_BASE_URL` or `SITE_ADDRESS` is empty, and names the missing one. Generate them with `openssl rand -base64 48`.
- **Payments and AI:** set the Stripe and OpenAI variables. Without them, checkout and AI descriptions fail with a clear message; the Sandbox and the mock never run in Production.
- **Client IP:** the API believes `X-Forwarded-For` only from Caddy's fixed address (`CADDY_IP`, default `172.30.250.10` on `INTERNAL_SUBNET` `172.30.250.0/24`; change both if that subnet clashes with a host network). Rate limits therefore count per real client.
- **Upgrades:** `git pull`, take a database backup, then run the `up --build -d` command again. Migrations run when the new API starts.
- **Backups:** the data is in the `olhai_pgdata` (database) and `olhai_storage` (photos, PDFs) volumes. For example: `docker compose -f docker-compose.prod.yml --env-file .env.production exec db pg_dump -U inspectflow inspectflow > backup.sql`.

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
| `WEB_BASE_URL` | Base URL used in tenant invitation and share links |
| `MIGRATE_ON_STARTUP`, `SEED_DEMO_DATA` | Start-up behaviour (seed is ignored outside Development) |
| `TRUST_FORWARDED_HEADERS` | Honour `X-Forwarded-For` / `X-Forwarded-Proto` (only when the API is reachable exclusively via a trusted proxy) |
| `TRUSTED_PROXIES` | Required with `TRUST_FORWARDED_HEADERS=true`: comma-separated IPs or CIDR ranges of that proxy (e.g. `172.30.250.10`). Headers from any other sender are ignored; an empty or invalid list stops the start-up. Never list the web container: the Next.js rewrite passes a client-supplied `X-Forwarded-For` through |
| `WEB_BIND_ADDRESS` (dev compose) | Host address the web app is published on: `127.0.0.1` (default) or `0.0.0.0` for the LAN. db and API are always on `127.0.0.1` |
| `SITE_ADDRESS`, `HTTP_PORT`, `HTTPS_PORT`, `CADDY_IP`, `INTERNAL_SUBNET` (prod compose) | Caddy site (domain → automatic HTTPS, or `:80`), published ports, and the proxy's fixed internal address (see *Production deployment*) |
| `CORS_ALLOWED_ORIGINS` | Comma-separated origins for browser clients on other origins (not needed for the bundled web app) |
| `API_INTERNAL_URL` (frontend, build time) | Where Next.js proxies `/api/*` |
| `BILLING_PROVIDER` | `Auto` (default: Stripe when `STRIPE_SECRET_KEY` is set, otherwise the Sandbox in Development), `Stripe` or `Sandbox` |
| `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, `STRIPE_PRICE_ID` | Stripe settings (server side only). The price id belongs to the first plan (`Billing:Plans:0:StripePriceId`) |

Business limits (`Inspections__MinimumGeneralPhotosPerRoom`, `MaxPhotosPerRoom`, `MaxUploadBytes` (15 MB), tenant invitation and share-link lifetimes) and rate limits (`RateLimiting__AuthPerMinute`, …) are in `backend/src/Api/appsettings.json`.

### OpenAI

1. Set `OPENAI_API_KEY` (environment variable, `.env`, `dotnet user-secrets` or your secret store — never commit it).
2. Optionally set `OPENAI_MODEL` (default `gpt-4.1-mini`; any vision model supporting structured outputs).
3. Restart the API. `GET /api/ai/status` shows the active provider (the key is never returned).

The key is used only by the API process, only in the `Authorization` header of outbound requests; it is never sent to the browser, stored in the database or written to logs/audit metadata. Prompts are in `backend/src/Modules/AI/Application/AiPrompts.cs`.

### AI writing guide and output language

- **Writing guide:** `backend/src/Modules/AI/Guidelines/inspection-writing-guide.md` defines how the AI reads the photos and writes, **as the inspector's own report entry** (never "the image shows"): an opening sentence about the room, walls and floor always covered, defects described on the surface they are on (including close-ups among the room photos), reading order, condition criteria, materials, room/defect/comparison structure, room checklists, style, before/after examples and Brazilian Portuguese vistoria terminology. It is sent with every analysis, after the non-negotiable safety rules.
- **Improving the drafts:** find drafts that inspectors heavily rewrote (compare `ai.ai_analyses.description` with the final texts) and add a general rule or a short before/after example. Bump `guide-version` on the first line, add a line to the guide's *Revision log* and run `dotnet test`, which rejects a guide without its version or marker. Only the part below `=== GUIDE FOR THE MODEL ===` is sent to the model.
- **Output language:** AI texts are written in the language of the inspector who requests them — the PT | EN switch, sent as `Accept-Language` (`en` → British English, `pt-BR` → Brazilian Portuguese; no choice → English). It is stored on each analysis (`output_language`) because the background worker has no request ([ADR 0026](docs/architecture/0026-ai-language-follows-the-switch.md)). JSON keys and condition/classification codes stay in English.
- **Tracking:** each analysis stores `prompt_version = <code>+g<guide>+<language>`, e.g. `2026-10-v3+g2026-10-17.1+pt-BR`, so quality can be compared per guide version. See [ADR 0013](docs/architecture/0013-ai-writing-guide-and-language.md).

### Languages

- **UI language:** `en` or `pt-BR`. The `locale` cookie set by the sidebar PT | EN switch (or the EN / PT selector on public pages) wins; otherwise the browser's `Accept-Language` decides (any `pt*` → `pt-BR`, anything else → English). Texts live in `frontend/src/i18n/messages/*.ts`; the Portuguese object must have every English key, so a missing translation fails `npx tsc --noEmit`.
- **API messages** follow the `Accept-Language` header, which the web app sends with every call. Error `code` values do not change; `title`, `errors` and `details` are translated. Without the header the API answers in English.
- **Report language** (company-wide, Owner/Admin, *Settings* page or `PUT /api/companies/me/report-language`): the PDF labels and report texts use it (AI drafts follow the inspector's switch instead, ADR 0026). It is frozen into each report at finalization, so changing it only affects future reports. New companies start in the language the owner is using.
- **Plans:** optional `Billing:Plans[i]:Translations:pt-BR` with `Name`, `Description` and `Features`; missing fields fall back to English.
- See [ADR 0014](docs/architecture/0014-localization.md).

### Subscriptions and payments

- **Plans** are configured under `Billing:Plans` in `backend/src/Api/appsettings.json`. Today there is one plan: **Professional, €49 / month**. Plans are served publicly at `GET /api/billing/plans` in the request language (see *Languages*), and the landing page and checkout render them, so adding a plan needs configuration only.
- **Who pays:** `Billing:SubscriptionRequiredRoles` (default `["Company"]`). `Billing:GraceDays` (default 7) keeps access after a period ends or a renewal fails. `Billing:CheckoutMinutes` (default 60) is how long an abandoned checkout can be resumed.
- **Enforcement:** every Company API call checks the subscription in the database and answers **402** `subscription.required` without an active one. The web app routes such users to `/subscription/required`. See [ADR 0012](docs/architecture/0012-subscription-billing.md).
- **Stripe setup:**
  1. Create a recurring Price and set `STRIPE_PRICE_ID`.
  2. Set `STRIPE_SECRET_KEY`.
  3. Add a webhook endpoint `https://<api-host>/api/billing/webhooks/Stripe` for `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, `checkout.session.expired`, `invoice.paid`, `invoice.payment_failed` and `customer.subscription.deleted`, and set its signing secret as `STRIPE_WEBHOOK_SECRET`.
  4. Make sure `WEB_BASE_URL` is correct, because it is used for the success and cancel URLs.
- **Sandbox (development):** without Stripe, *Continue to payment* opens `/checkout/sandbox/{id}`, which is clearly labelled as test mode. Card `4242 4242 4242 4242` approves and `4000 0000 0000 0002` declines. An approved payment makes the sandbox send a signed webhook through the same processing path as Stripe. The sandbox is disabled outside Development unless `Billing__Sandbox__AllowOutsideDevelopment=true`; with no provider, checkout fails with a clear message.

### Mock AI mode

Without a key (or with `AI_PROVIDER=Mock`) in Development, `MockImageAnalysisService` returns deterministic, plausible structured results. Every text starts with **"[Development mock AI — photos were not actually analysed]"** and the UI shows a "Development mock AI" badge. Outside Development the mock is disabled unless `Ai__AllowMockOutsideDevelopment=true`; AI requests then fail with a clear message and inspectors write descriptions manually.

## Demo users (Development only)

Seeded automatically when the environment is `Development` and the database is empty. Password for all: **`Demo@12345`**.

| Role | Email | Seeded data |
|---|---|---|
| Company | `company@demo.local` | Active subscription (seeded, 1 year); "Demo Property Management"; **12 Main Street** (6 rooms, tenancy, **open public Move In**); **48 Oak Avenue** (6 rooms, tenancy, **finalized & accepted Move In** with photos, a defect, report + PDF — ready for a Move Out) |
| Agent | `agent@demo.local` | "Demo Inspector" (did the 48 Oak Avenue Move In); account created by the demo company, so it is in its **team** (databases seeded earlier get the same link) |
| Tenant | `tenant@demo.local` | "Demo Tenant", signed up from the first tenancy's invitation and accepted the second |

Demo credentials are never created outside Development.

**Switching accounts (Development only):** the header shows a **Dev: switch account** dropdown that signs you in as any of the demo users above in one click. It uses `GET /api/dev/accounts` and `POST /api/dev/switch`, which the API maps only when `ASPNETCORE_ENVIRONMENT=Development`; in any other environment they return 404 and the dropdown is hidden.

## How to test the flows

**Languages.** Open http://localhost:3000 in a browser set to Portuguese (or pick **PT** in the header): the landing page, sign-in, checkout and every area switch to Portuguese, and the choice survives a reload. A wrong sign-in shows "E-mail ou senha inválidos." As the company, open *Settings* (*Configurações*) and set **Report language** to Português (Brasil): the next finalized report and its PDF are in Portuguese, while earlier reports keep their language. As the agent, switch **PT**: room names such as "Living Room" / "Bedroom 1" show as "Sala de estar" / "Quarto 1" (names the company typed itself stay as typed), and AI descriptions requested now are written in Portuguese; switch **EN** and request again for English (use `AI_PROVIDER=Mock` to avoid OpenAI costs — the mock label names the language).

**Landing page and routing.** Open http://localhost:3000 in a private window: the landing page renders immediately (no sign-in request). Sign in (any demo user), then open http://localhost:3000 again: you land on your dashboard without seeing the landing page, also after a reload. *Sign out* and open `/` once more: the landing page is back. On the landing page itself, every call to action leads somewhere real: *Get started* → company sign-up, *See how it works* → the process section, *Inspector sign-in* → `/login`, *Get started today* → company sign-up, *Sign in* → `/login`. Narrow the window below 1024px for the menu (Escape closes it).

**Landing and subscription.** Open http://localhost:3000 and click *Get started*. Register the company (`/register?plan=professional`), then *Continue to payment* on `/checkout`. On the sandbox page, pay with `4242 4242 4242 4242`. `/subscription/success` confirms once the webhook has been processed and takes you to the workspace onboarding.
*Abandoned checkout:* register, leave the payment page, then sign in again later. You land on **Subscription required** → *Continue to payment* resumes the same checkout → pay → dashboard. `/pricing` shows the plans on their own page, and `/app` sends a signed-in user to their home.

**Company** — sign in as `company@demo.local` (or register a Company account → create workspace). Dashboard → *Add property* (rooms editor) → property page: edit rooms, create a tenancy, invite a tenant (copy the invitation link) → *New inspection* → choose type/tenancy → *Who can take it*: **Public** (whole team) or **Direct** (pick the agent) → publish.
Shortcut: on *Add property*, tick **Also create an inspection for this property** — the inspection form (type Move In / Periodic / Other, tenancy start/end date, Public or Direct, dates, instructions, publish now) becomes active and one button creates the property, the tenancy (when a start date is given; required for Move In) and the inspection. Move Out is not offered there because a new property has no Move In to compare with. If the inspection step fails, the property stays saved and confirming again only retries the inspection.

**Agent** — sign in as `agent@demo.local` (best on a phone or a narrow browser window). *Available* → open → *Accept inspection* → *Start inspection*, then two pages:
1. **Photos** (`/agent/inspections/{id}/capture`) — every room in sequence on one page: *Take photo / Upload photos* for the room, *+ Add defect* and the defect's photos. No text is typed here. *Continue to descriptions* checks that every room and defect has a photo, then requests every missing AI text at once (room descriptions, defect descriptions, Move Out comparisons) and moves on. It is disabled while uploads are running.
2. **Descriptions** (`/agent/inspections/{id}/descriptions`) — each room shows "Writing the description…" until its texts arrive (the page polls), then the editable room description, defects (label, location, description, classification, *I confirm this defect*), the Move Out comparison and agent notes, all autosaved. *Complete rooms & review* saves pending edits, completes every room (listing anything that blocks it) and submits for review.

Then *Review inspection* → edit texts if needed → *Finalize inspection* → report page with *Download PDF*.

**Tenant** — tenant accounts are created only from an invitation ([ADR 0023](docs/architecture/0023-tenant-accounts-from-invitations.md)). Open the invitation link from the company → *Create account* (`/register?invite=…`) → sign up with the invited email: you join the tenancy at once (another email is refused, and registering `role: Tenant` without a token returns 400). A tenant who already has an account signs in from the invitation and clicks *Accept invitation*. Or sign in as `tenant@demo.local`. *My inspections* → open a report awaiting review → *Add observation about this room* / *I want to add observations* → *Everything is correct* or *I disagree* (reason required).

**Agents (created by the company)** — `/register` has no Inspector option, and the API refuses `role: Agent` (400). As the company, open *Agents* (`/company/agents`) and register an agent: name, email and an initial password create the account (an email already used by any account, e.g. `agent@demo.local` or `tenant@demo.local`, is refused with the same message). Sign in as the new agent with that password. Then, as the company, *New inspection* → *Who can take it*:
- **Public**: the inspection appears under *Available* for your company's agents only, and the first to accept takes it. Agents of another company never see it (try with an agent created by a second company account).
- **Direct** → pick the agent (required): after publishing it is already **Assigned**. The agent finds it under *My inspections* (and in notifications) and only has to *Start inspection*. Colleagues do not see it.
- On the inspection page, the **Team assignment** panel sends it to another agent or *Releases to the team* until the agent starts, also when an agent accepted a Public one. Removing an agent from the team hides future Public inspections from them; inspections already assigned stay with them.

**Move In** — as above on 12 Main Street (seeded open inspection) or any new property with a tenancy.

**Move Out** — as the company: *New inspection* for **48 Oak Avenue** → type *Move Out* → the finalized Move In is pre-selected under *Compare with* → publish. As an agent: accept/start; on the photos page every room can expand its read-only **Move In record** (text, photos, defects). Add photos and defects; on the descriptions page the AI visual comparison has already been requested — use *Compare recorded data* if useful and pick your decision (Unchanged, Normal wear, New damage, …) and save it — a room can't be completed without it. Finalize: the report and PDF contain the comparison summary. The tenant then reviews it.

## Tests

```bash
cd backend
dotnet test                                   # needs Docker (Testcontainers starts PostgreSQL)
# or against an existing server (a temporary database is created and dropped):
TEST_DATABASE_CONNECTION_STRING="Host=localhost;Port=15432;Database=postgres;Username=inspectflow;Password=inspectflow" dotnet test

cd frontend && npm run lint && npm run build

# PDF report previews for page-by-page review (7 scenarios: PDFs + one PNG per page)
cd backend && PDF_PREVIEW_DIR=/tmp/pdf-preview dotnet test --filter "FullyQualifiedName~ReportPdf"

# Full UI flow (Move In + Move Out, 3 roles) against a running stack; screenshots in e2e/output
# (languages: Portuguese browser, switcher + cookie, Portuguese API error, sidebar PT | EN switch; landing + subscribe, abandoned checkout → sign in → resume,
#  landing page: server render, every call to action, viewports 375-1920, axe WCAG 2.2 AA, signed-in redirect / reload / logout / expired session;
#  Move In + Move Out with an agent created by the company, a second agent getting a Direct inspection hidden from the first; uses the Sandbox payment provider)
cd e2e && npm install && BASE_URL=http://localhost:3000 node ui-flow.mjs
```

262 backend tests cover: AI drafts in the requester's language (PT and EN switch against a company in the other language, English without a choice), trusted proxies (addresses and CIDR ranges, empty or invalid lists refused with the value, IPv4-mapped proxy addresses, X-Forwarded-For from the web container or loopback ignored), photo GPS removal (EXIF in JPEG/PNG/WebP in both byte orders, XMP values, PNG CRCs, date taken and orientation kept, malformed and truncated files untouched, stored hash = stored bytes), sign-in protections (per account + device throttle without account lockout, locked state only revealed with the right password, the same answer for unknown emails and wrong passwords), tenant sign-up only from an invitation (token required, invited email only, single use) and further invitations accepted while signed in, agents belonging to their company (no agent sign-up, accounts created by the company and never linked, one message for every used email, Public inspections hidden from and not acceptable by other companies' agents, Direct inspections hidden from colleagues, taking back an accepted inspection until the agent starts), the AI report voice (room overview, walls and floor, defect surface and inspector label, no "image" wording, pt-BR terminology, guide word budget, mock drafts), the product name on the PDF report (Olhai, not the code name), the session hint cookie behind the routing of `/` (set, renewed, removed on logout and failed refresh, kept when a parallel tab rotated the token), company team agents (account creation, direct assignment, hidden designated drafts, reassignment/release until start, removal), the PDF report (7 layout scenarios render, missing photos become placeholders, Move In photos loaded for Move Out reports), languages (Accept-Language resolution incl. q-values and pt-PT, translated ProblemDetails titles/errors/details, Identity and AI errors, 402 in Portuguese, plans per language, report language frozen in the snapshot, PDF labels and dates in both languages, v1 snapshots read as English), the AI writing guide (parsing, prompt order safety → language → guide, output language in the OpenAI system message, prompt version), company report language and localization, subscriptions (no access without an active subscription, 402 on the Company API, the full sandbox payment, resuming an abandoned checkout, success-page parameters and forged or unsigned webhooks never activating, replayed webhooks applied once, past due → grace → cancellation, lapsed → subscribe again, parallel checkout starts, access policy and state machine, Stripe request/signature/event mapping), authorization across companies/roles/tenants, property & rooms, room snapshot, publish rules, public accept (+ 8 parallel agents → exactly one wins), state transitions, upload validation (content sniffing, size, ownership, after-finalization), room completion and blocked finalization, finalization + PDF, report immutability (hash, EF guard, DB trigger), share links, tenant access/observations/accept/dispute (+ concurrent decisions), completed rooms reopening when an edit breaks a rule, same-tenancy baselines, the full Move In and Move Out flows through HTTP, the OpenAI adapter contract (strict schema, safety prompt, no key leakage) and architecture boundaries.

## API overview

| Area | Endpoints (prefix `/api`) |
|---|---|
| Auth | `POST auth/register` (`Company`, or `Tenant` with `invitationToken` and the invited email), `auth/login`, `auth/refresh`, `auth/logout`, `GET auth/me` (includes `subscription`). Web clients get the refresh token as the `if_refresh` cookie (scoped to `/api/auth`) plus the no-secret `if_session` hint (`Path=/`) used to route `/` |
| Billing | `GET billing/plans` (public), `GET billing/subscription`, `POST billing/checkout`, `GET billing/checkout/{id}`, `POST billing/webhooks/{provider}` (signed), `GET billing/sandbox/sessions/{id}`, `POST billing/sandbox/sessions/{id}/pay` (Sandbox only) |
| Company | `POST companies` (optional `reportLanguage`: `en` or `pt-BR`), `GET companies/me`, `PUT companies/me/report-language`, `GET/POST companies/me/agents`, `DELETE companies/me/agents/{agentId}`, `GET company/dashboard`, `GET/POST properties`, `GET/PUT properties/{id}`, `POST/PUT/DELETE properties/{id}/rooms[/{roomId}]`, `PUT properties/{id}/rooms/order`, `GET properties/{id}/tenancies`, `POST tenancies`, `POST tenancies/{id}/tenants`, `GET/POST inspections`, `GET/PUT inspections/{id}` (create takes an optional `assignedAgentId`: set → Direct, absent → Public; create and publish return the inspection details), `PUT inspections/{id}/assignment` (`{ agentId }`, or `null` to release it to the whole team until the agent starts), `POST inspections/{id}/publish|cancel|send-to-tenant`, `POST reports/{id}/share-links` |
| Agent | `GET agent/dashboard`, `GET agent/available[/{id}]` (the agent's company's Public inspections only), `POST agent/inspections/{id}/accept|start|submit-review|return-to-progress|finalize`, `GET agent/inspections/{id}/review`, `GET/PUT agent/inspections/{id}/rooms/{roomId}`, `POST .../photos|analysis|complete|reopen|defects|comparison/basic|comparison/analysis`, `PUT .../comparison/decision`, `PUT/DELETE .../defects/{defectId}` |
| Tenant | `GET tenant/dashboard`, `GET tenant/invitations/{token}`, `POST tenant/invitations/{token}/accept`, `GET tenant/inspections/{id}/report`, `POST tenant/inspections/{id}/observations|accept|dispute` |
| Shared | `GET inspections/{id}/report`, `GET reports/{id}`, `GET shared/reports/{token}`, `GET files/{key}?exp&sig`, `GET ai/status`, `GET notifications`, `GET /health` |

Errors are RFC 7807 problem details with a stable `code` (e.g. `inspection.invalid_transition`, `room.incomplete` with `details`).

## Security notes

- Authorization is enforced in application services (membership/assignment checks on every query); the UI only hides what the API already forbids.
- Tokens (tenant invitations, share links, refresh tokens): SHA-256 of 256-bit random values; plaintext shown once.
- Rate limits on sign-in/registration, tenant invitation acceptance, public endpoints, uploads and AI. Failed sign-ins are throttled per account **and** device (5 per 15 minutes → 429), and the account-wide Identity lockout needs 20, so nobody can lock someone else out from one machine. An unknown email and a wrong password get the same answer, and "locked" is only shown with the right password (ADR 0009, update 2026-10-17).
- Per-IP limits use the real client IP only through a listed proxy (`TRUSTED_PROXIES`); a client-supplied `X-Forwarded-For` from anyone else is ignored. In production, Caddy is the only entry point; database, API and web app are not published ([ADR 0024](docs/architecture/0024-production-deployment-and-trusted-proxies.md)).
- Uploads validated by magic bytes (JPEG/PNG/WebP), size and ownership; the GPS position (EXIF and XMP) is erased in place before hashing and storing, while date taken and orientation stay ([ADR 0025](docs/architecture/0025-photo-gps-removal.md)); stored under server-generated keys; served via expiring HMAC-signed URLs with `nosniff`.
- `Referrer-Policy: no-referrer` (tokens appear in tenant invitation and share-link paths), `X-Frame-Options: DENY`.
- Audit log (`audit.audit_logs`, append-only) records the key events; metadata containing secret-like keys is refused.

## Known limitations & technical debt

- **Rate limiting in development**: the Next.js rewrite does not forward the client IP, so in the development stack every per-IP limit applies to the web container's IP. The production stack routes `/api` through Caddy and counts per real client (ADR 0024), the per-device sign-in throttle included. In-memory limits assume one API instance; several instances need a shared store. The Next.js proxy (development only) has a 30 s timeout per request.
- **AI queue is in-process**: durable in the database and re-queued at start-up, but with several API instances use a real queue/worker.
- **Local storage only**; S3/Azure Blob implementations and direct-to-bucket uploads are prepared but not written. Photos are not resized server-side (the web client downsizes before upload; the PDF uses 144 DPI rasterization).
- **Notifications** are stored in-app and logged; no email/push delivery yet (invitation links are shown to the company to share).
- **Billing:** there is no customer self-service yet (Stripe billing portal, card update, cancellation). A lapsed subscription is evaluated when it is read, with no background expiry job. The Stripe adapter has not been exercised against a live Stripe account (contract tests only). Report pages (`/reports/...`) stay readable for their authorized viewers regardless of subscription.
- No email verification, password reset, MFA, or company staff management UI (roles/permissions exist in the model). Tenant addresses are protected by the invitation link instead (ADR 0023); a company account can still be created with an address its owner does not control. A company creating a team agent account chooses its initial password and shares it with the agent itself.
- Multiple tenants: the first decision (accept/dispute) closes the review. No dispute-resolution workflow or report re-issuing yet (versioning supports it).
- Rooms cannot be excluded per inspection (all active rooms are snapshotted; `IsRequired` prepared).
- PDF: there is no "assessment limited" marker yet, because the snapshot has no reliable flag for it. Short reports leave some empty space at the bottom of the cover.
- PDF generation runs synchronously during finalization (≈1–2 s for typical reports); very large reports should move to a background job.
- Accessibility and i18n are basic (English UI, en-IE formats).
- EF logs one `fail` line on a brand-new database (migrations history table lookup) — harmless.

## Deliberately out of scope for this MVP

Native mobile apps, multiple plans / coupons / invoices UI, agent ratings, real-time chat, own ML / fine-tuning, microservices, Kubernetes, distributed queues, mandatory geolocation, legal e-signatures, automatic liability decisions, integrations with external agencies.
