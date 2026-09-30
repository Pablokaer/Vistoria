# Changelog

Every code change is recorded here (newest first), together with the docs it touched.

## 2026-09-30

- **Add property + inspection in one form.** *Add property* has an **Also create an inspection for this property** checkbox. Ticking it enables the inspection form (Move In / Periodic / Other, tenancy start/end date, visibility, dates, instructions, publish now); one button creates the property, the tenancy (if a start date is given; required for Move In) and the inspection. A failed inspection step keeps the property and retries only the inspection. The shared inspection fields live in `frontend/src/components/InspectionForm.tsx`, also used by *New inspection*.
- **Dev account switcher (Development only).** Header dropdown **Dev: switch account** signs in as the seeded company, agent or tenant. Backed by `GET /api/dev/accounts` and `POST /api/dev/switch`, mapped only when `ASPNETCORE_ENVIRONMENT=Development` (404 elsewhere, covered by a test).
- **Two-step agent flow.** One photos page for every room and its defects, then one descriptions page; all AI texts are requested in a single batch when leaving the photos page. See [ADR 0011](architecture/0011-capture-then-describe.md). The per-room page was removed.
- **`run-project.sh`** launcher: `up` (default), `stop`, `reset`, `logs`.
- Initial import of the MVP.
