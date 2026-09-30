# 0007 — Report immutability

**Context.** A finalized report is evidence. It must be reproducible exactly as issued even if properties, users or rooms change later, and must never be silently rewritten.

**Decision.**
- Finalization (single transaction, exclusive row lock) builds a `ReportSnapshot` (company, property, inspection, agent, tenants, rooms, photos by storage key, defects, comparison) → stored as JSON text in `InspectionReportVersion` with its SHA-256, plus the rendered PDF (storage key + SHA-256).
- Web page and PDF are rendered **only** from the snapshot.
- `InspectionReportVersion` is append-only: EF `SaveChanges` guard + PostgreSQL triggers reject `UPDATE`/`DELETE` (also for audit logs and tenant responses/observations). A correction would be a new version (`AddVersion`), never an update.
- After finalization every content-changing endpoint rejects changes (`inspection.finalized`); photo objects are write-once.
- Tenant observations/decisions are stored next to the report, referencing the version they were made on.

**Consequences.** Reports can be verified by hash. Some data is duplicated. Issuing corrected versions (e.g. after a dispute) is a future feature with the model already in place.
