# 0001 — Modular monolith

**Context.** An MVP with a small team, three user types and a single web client today, native apps later. Microservices would add distributed transactions (accept, finalize), deployment and observability cost with no benefit at this scale.

**Decision.** One ASP.NET Core API, one PostgreSQL database.
- `src/Shared` — kernel (errors, clock, current user, token helpers).
- `src/Modules` — business modules as folders: `Identity, Companies, Properties, Tenancies, Inspections, Media, AI, Reports, Tenants, Notifications, Audit`. Each has `Domain/` (pure C#: entities, rules, state machine) and `Application/` (use-case services, DTOs, abstractions such as `IStorageService`, `IImageAnalysisService`, `IPdfService`, `INotificationService`).
- `src/Infrastructure` — EF Core `AppDbContext`, migrations, Identity/JWT, local storage, OpenAI + mock AI, QuestPDF, background workers, seed.
- `src/Api` — HTTP endpoints per module, auth policies, rate limiting, error mapping.
- Each module owns a **PostgreSQL schema** (`inspections.*`, `reports.*`, …) so boundaries are visible in the data too.

**Consequences.**
- Critical operations stay in a single local transaction.
- Boundaries are enforced by convention plus `ArchitectureTests` (domain types must not reference EF Core, Npgsql, QuestPDF, HTTP or Infrastructure).
- Modules share one `IAppDbContext`; cross-module *reads* are allowed (e.g. Inspections reads media counts), cross-module *writes* go through the owning module's service. Splitting a module out later means replacing those reads with an API/contract — the schema split already exists.
- Modules live in one assembly for simplicity; if boundaries erode, promote each module folder to its own project (the namespaces already match).
