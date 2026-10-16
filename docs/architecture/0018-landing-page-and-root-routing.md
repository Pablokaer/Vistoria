# 0018 — Landing page design and session-aware routing of "/"

**Context.**

- **Routing.** "/" is the public landing page. Signed-in users should go straight to their area, without a flash of the landing page, and anonymous visitors should not pay for an authentication round trip. The web app cannot tell on its own whether a session exists. The refresh token lives in an HttpOnly, SameSite=Strict cookie scoped to `/api/auth` (ADR 0009), so the Next.js server never sees it on page requests, and the access token lives only in memory. Before this change every page, the landing included, called `/api/auth/refresh` on load. Anonymous visitors therefore got a 401 in the console, and signed-in users saw the landing page with an "Open dashboard" button.
- **Design.** The previous landing page followed a generic SaaS template: a 7-step card grid, 12 feature cards and brand blue everywhere.

**Decision.**

- **Session hint cookie.** Whenever the API issues tokens (sign-in, registration, refresh, dev switch), it also sets `if_session=1`. The cookie is HttpOnly, `Path=/`, `SameSite=Lax` (so links from emails still count) and expires with the refresh token. It carries no secret. Logout deletes it, and so does a failed refresh, through `Response.OnStarting`, because the error handler clears headers set before the exception. There is one exception: a token that a parallel tab rotated a moment ago gets the 401 code `auth.refresh_superseded` and keeps the hint, because that session is still alive.
- **The server reads the hint** (`lib/session-hint.ts`) in the root layout and passes it to `AuthProvider`. Without the hint the visitor is anonymous at once: no refresh request and no loading state.
- **"/" uses `SignedInRedirect`.**
  - No hint: the server renders the landing page directly. This is good for SEO and avoids any flash.
  - Hint present: only an "Opening your dashboard…" state renders until the session is checked, then the user goes to `homeFor(user)` (dashboard, onboarding or subscription page).
  - Stale hint (the refresh fails): the landing page appears and the hint is removed.
- **Unchanged.** `/pricing` stays public for everyone. Protected routes keep their existing guards (`RoleGate`, API authorization). The landing page renders only public content.
- **Visual language.** Public pages look like a paper record:
  - A warm paper surface (`--color-paper`) with hairline ink rules.
  - Navy primary actions, the same navy as the app's sidebar.
  - Newsreader, an editorial serif loaded only by the public pages through `next/font`, for headlines; Inter for body text.
  - The product's own Move In teal and Move Out orange as the only accents.
- **Content.** The hero shows a specimen of a finalized Move Out report built from the report's real parts: Move In/Move Out photos, a labelled AI draft, a defect with the inspector's decision, version and SHA-256 footer. The sections are:
  - A four-step workflow ledger.
  - The report guarantees: immutability, Move In comparison, people decide, sharing.
  - The three dispatch routes: team, marketplace, private invitation.
  - Pricing from the API.
  - A final call to action.
  - Every statement maps to an existing feature. There are no invented figures, customers or testimonials.

**Consequences.**

- Anonymous pages make no auth request, which removes the 401 console error and a round trip.
- Sessions that existed before this change have no hint. Those users sign in once more; after that the hint is maintained on every token issue.
- Native clients ignore the cookie.
- The extra font is only fetched on public pages.
- The e2e flow (`e2e/landing.mjs`) covers:
  - Server rendering for anonymous visitors, with no refresh call and no console errors.
  - The entry points.
  - Layouts at 375, 768, 1024 and 1440 px.
  - axe WCAG 2.2 AA (serious and critical violations fail).
  - The signed-in redirect without landing markup, reload, logout and the expired-session fallback.
