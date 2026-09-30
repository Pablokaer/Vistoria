# 0010 — Concurrency

**Decision.**
- `Inspection`, `InspectionInvitation` and `TenancyMember` use PostgreSQL `xmin` as an optimistic concurrency token. **Accept** (two agents), **tenant decision** (two tenants) and invitation attempt counting therefore resolve to exactly one winner; the loser gets `409`.
- Agent content edits (rooms, photos, defects, AI requests) take a `FOR SHARE` lock on the inspection row inside their transaction; **finalization** takes `FOR UPDATE`. An edit can never commit during/after finalization, while edits don't block each other.
- Child edits don't touch the inspection row, so parallel photo uploads don't conflict.
- AI results are applied with conditional single-statement updates (pre-fill only when empty) so they never overwrite a concurrent agent edit.
- Unique indexes back the invariants (one report per inspection, one version number per report, one comparison per room).

**Consequences.** Tested with parallel requests (`Concurrent_accepts_result_in_exactly_one_assignment`, `Concurrent_tenant_decisions_resolve_to_a_single_outcome`).
