# 0004 — Tenancy model

**Context.** A property is let many times; Move In / Move Out belong to one letting period; a letting can have several tenants who may not have accounts yet.

**Decision.** `Property → Tenancy → Inspection`. `TenancyMember` holds name + email and an optional `UserId`. The company invites by email; the tenant accepts a single-use, expiring link (token stored as SHA-256) while signed in **with the invited email**. Tenants see only inspections of tenancies they joined, and only once the report was sent to them (`AwaitingTenant`, `Accepted`, `Disputed`).

**Consequences.** No tenant data leaks by guessing ids (every query is scoped by membership). When several tenants exist, the first recorded decision wins (MVP); per-tenant consensus is future work. Email verification is not implemented — possession of the invitation link plus the matching email is the proof.
