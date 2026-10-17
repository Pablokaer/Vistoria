# 0024 — Production deployment and trusted proxies

**Context.**

- **Development defaults could reach production.** `docker-compose.yml` defaults to `ASPNETCORE_ENVIRONMENT=Development`. Run as is on a server, it exposes the dev account switcher (`/api/dev/switch`), the Sandbox payment page (test card 4242 activates a subscription), the mock AI and the `dev-only-*` secrets, and it publishes PostgreSQL on 5432 with the password `inspectflow`.
- **Per-IP rate limits never saw the client.** Every limit is partitioned by IP, but behind the Next.js rewrite every request comes from the web container's IP. All users therefore shared one budget: 30 sign-ins per minute across the whole platform, which one client could exhaust.
- **Turning on forwarded headers made it worse.** `TRUST_FORWARDED_HEADERS=true` cleared `KnownProxies`/`KnownIPNetworks`, so any sender's `X-Forwarded-For` was believed.
- **The Next.js 16 rewrite cannot carry the client IP.** It does not add the client IP and passes a client-supplied `X-Forwarded-For` through unchanged. This was verified with an echo server: a plain request arrived without the header, and `X-Forwarded-For: 6.6.6.6` arrived as `6.6.6.6`. Trusting the web container would let anyone pick their own bucket.

**Decision.**

- **Trusted proxies are explicit.** With `TRUST_FORWARDED_HEADERS=true` the API needs `TRUSTED_PROXIES` (`ForwardedHeaders:KnownProxies`): a comma-separated list of IP addresses or CIDR ranges. Only those senders' `X-Forwarded-For`/`X-Forwarded-Proto` are believed; the framework's loopback defaults are removed as well. An empty list or an entry that is not an address or range (including IPv4 shorthand such as `172.30.250`) stops the start-up with the offending value and the expected shape (`TrustedProxyList`). IPv4-mapped IPv6 connection addresses (Kestrel's dual-mode socket) match their IPv4 entry. The web container must never be listed.
- **Production has its own compose file.** `docker-compose.prod.yml` is standalone rather than an override of the development file, so a forgotten `-f` cannot bring in the Development environment, demo seed, `dev-only-*` secrets or published database. It sets `ASPNETCORE_ENVIRONMENT=Production` and `SEED_DEMO_DATA=false`, and requires `POSTGRES_PASSWORD`, `JWT_SECRET`, `STORAGE_SIGNING_KEY`, `WEB_BASE_URL` and `SITE_ADDRESS` (`${VAR:?…}`: compose refuses to start and names the missing one). Its project name is `olhai`, so it never shares containers or volumes with the development project in the same directory. Settings live in `.env.production`, which is gitignored; `.env.production.example` is the template.
- **Caddy is the only entry point.**
  - **Pinned image:** `caddy:2.11.4-alpine`.
  - **Published ports:** 80 and 443 (HTTP_PORT / HTTPS_PORT). Database, API and web app are published nowhere.
  - **Routing:** `/api/*` and `/health` go straight to `api:8080`; everything else goes to `web:3000`. Browser API traffic therefore never passes through the Next.js rewrite.
  - **Client IP:** Caddy trusts no upstream proxy, so it discards any client-sent `X-Forwarded-For` and sets the real client IP.
  - **Fixed address:** the proxy has a fixed address (`CADDY_IP`, default `172.30.250.10`) on an internal network with a fixed subnet (`INTERNAL_SUBNET`, default `172.30.250.0/24`). The API's `TRUSTED_PROXIES` is that address only.
  - **HTTPS:** a domain as `SITE_ADDRESS` gets automatic HTTPS. Use `:80` behind another TLS terminator.
  - **Request bodies:** capped at 25 MB on the API routes (uploads are 20 MB at most).
  - **Headers:** HSTS (`max-age=31536000`) is sent.
- **Migrations run at start-up in production (`MIGRATE_ON_STARTUP=true` by default).**
  - **One instance.** The stack runs exactly one API instance; its in-memory rate limiter and in-process AI queue already assume that.
  - **Schema in step with the image.** Migrating at start-up applies a new image's migrations before it serves requests, with no separate step to forget.
  - **Concurrent starts are safe.** EF Core holds a database lock while migrating.
  - **Mitigations:** migrations are reviewed in code review, and the README asks for a `pg_dump` before every upgrade.
  - **Opting out:** operators who want to apply a reviewed idempotent script themselves set `MIGRATE_ON_STARTUP=false`.
- **The development stack stays on the machine.** `docker-compose.yml` publishes db and API on `127.0.0.1` only, PostgreSQL on host port 15432 (`POSTGRES_PORT`), because a locally installed PostgreSQL on `0.0.0.0:5432` makes a loopback bind of 5432 fail on Windows. The web app is on `127.0.0.1` too unless `WEB_BIND_ADDRESS=0.0.0.0` opens it to the LAN (for example to try it on a phone). In development the per-IP limits still see the web container's IP; that is accepted there.

**Consequences.**

- In production every per-IP limit (sign-in, refresh, public, invitation) is per real client, and a spoofed `X-Forwarded-For` lands in the sender's own bucket. Verified on a local run of the production stack: 30 sign-ins sent with `X-Forwarded-For: 1.1.1.1`, then one with `2.2.2.2`, gave 429, and the IP the API recorded for a new session was the caller's, not the forged one.
- A deployment that enables forwarded headers without listing its proxy fails fast instead of silently trusting everyone.
- Changing the internal subnet means changing `INTERNAL_SUBNET` and `CADDY_IP` together; the API's trusted address follows `CADDY_IP` automatically.
- Uploaded files and the database live in named volumes (`storage`, `pgdata`); backups are the operator's job (README).
- Still open: scaling beyond one API instance needs a shared rate-limit store and a real queue (README, known limitations).
