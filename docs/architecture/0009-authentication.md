# 0009 — Authentication and authorization

**Decision.** ASP.NET Core Identity (users, roles `Company`/`Agent`/`Tenant`, PBKDF2 passwords, lockout) + short-lived JWT access tokens (15 min) + opaque refresh tokens (14 days, SHA-256 at rest, rotated on every use, re-use of a rotated token revokes the family). Web clients (`X-Client: web`) receive the refresh token only as an `HttpOnly; SameSite=Strict` cookie scoped to `/api/auth`; the access token lives in memory. Native apps receive the refresh token in the body. The Next.js app proxies `/api/*` so everything is same-origin.

Capabilities are **roles + memberships** (`CompanyMember` with a company role — Owner today; Admin/PropertyManager/Employee/Viewer permissions already defined — `TenancyMember`, assigned `AgentId`). Every query is scoped server-side by the caller's identity; ids from the client are never proof of access (unknown or foreign ids → 404).

**Consequences.** Adding company staff roles is data, not schema. Email verification, password reset and MFA are not implemented yet.

**Update 2026-10-17 — sign-in abuse.** A security review found that the account lockout (5 failures → 10 min) let anyone lock a stranger out just by knowing the email, and that the sign-in answers revealed which addresses have accounts (the "locked" message came before the password check, and an unknown email answered faster). Sign-in now goes through `PasswordSignInGuard`:

- **Per-device throttle.** `ILoginThrottle` counts failures per **account + client IP** (`MemoryLoginThrottle`, in process like the HTTP rate limiter). After 5 failures in 15 minutes (`Auth:LoginThrottle`), that device gets 429 `too_many_attempts` for that account, even with the right password. Other devices are not affected. A successful sign-in resets the count.
- **Account-wide lockout raised to 20 failures / 15 minutes.** It still stops guessing spread over many machines, but one device can no longer reach it.
- **No account oracle.**
  - An unknown email and a wrong password get the same 401 text and code, and an unknown email spends the same hashing time against a throwaway hash.
  - The lockout message is only shown after the correct password.
  - Unknown emails are throttled like real ones.
- **Limits.** The throttle is keyed by the client IP, so it needs the real IP. In production that comes from the trusted reverse proxy (ADR 0024). Behind the bundled Next.js rewrite, every visitor shares the web server's IP. A distributed attacker can still trigger the account-wide lockout.
