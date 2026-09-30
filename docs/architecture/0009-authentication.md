# 0009 — Authentication and authorization

**Decision.** ASP.NET Core Identity (users, roles `Company`/`Agent`/`Tenant`, PBKDF2 passwords, lockout) + short-lived JWT access tokens (15 min) + opaque refresh tokens (14 days, SHA-256 at rest, rotated on every use, re-use of a rotated token revokes the family). Web clients (`X-Client: web`) receive the refresh token only as an `HttpOnly; SameSite=Strict` cookie scoped to `/api/auth`; the access token lives in memory. Native apps receive the refresh token in the body. The Next.js app proxies `/api/*` so everything is same-origin.

Capabilities are **roles + memberships** (`CompanyMember` with a company role — Owner today; Admin/PropertyManager/Employee/Viewer permissions already defined — `TenancyMember`, assigned `AgentId`). Every query is scoped server-side by the caller's identity; ids from the client are never proof of access (unknown or foreign ids → 404).

**Consequences.** Adding company staff roles is data, not schema. Email verification, password reset and MFA are not implemented yet.
