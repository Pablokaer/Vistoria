# 0015 — A small in-house design system and a sidebar app shell

**Context.** The app worked, but it looked like a generic admin template: a horizontal navbar, the same white bordered card around everything, four bare number boxes, and large empty placeholder boxes. Status was shown by colour only, and there were no icons in the app. The inspector's screens are used on a phone while walking through a property. There was no shared vocabulary for spacing, type, colour or feedback, so every page chose its own raw Tailwind palette values.

**Decision.**
- **Tokens live in `app/globals.css` (`@theme`, Tailwind 4)** and pages use their names, not raw palette colours:
  - a type scale (`text-page`, `section`, `card-title`, `body`, `label`, `caption`, `metric`);
  - neutrals (`ink`, `ink-2`…`ink-4`, `line`, `canvas`, `surface`);
  - a brand blue scale used for identity and the primary action only;
  - semantic tones (`success`, `warning`, `danger`, `info`, `review`, `neutral`);
  - inspection-type identities (Move In teal, Move Out orange, Periodic indigo);
  - modest radii, hairline shadows, layout widths and short motion.
- **Font:** Inter via `next/font`, self-hosted at build time, so the browser makes no request to Google.
- **No UI library.** Primitives live in `components/ui/*` (Button, Card, PageHeader, MetricCard, StatusBadge, InspectionTypeTag, EmptyState, Skeleton, Tabs, ProgressBar/Ring, Switch, Toast, useConfirm) and icons in `components/icons.tsx` (inline SVG, one stroke weight).
  - A status always shows an icon and a label, never colour alone.
  - Feedback uses toasts, and confirmations use an in-app dialog instead of `window.confirm`.
- **App shell** (`components/shell/*`):
  - Desktop: a compact navy sidebar with navigation grouped per role, account links, notifications, profile, language and sign-out. Only destinations the role can use are shown.
  - Phones and tablets: a top bar, a drawer with the same sidebar, and a bottom tab bar when the role has more than one destination.
  - The Development-only account switcher is a dashed monospace chip, kept apart from the product UI.
  - Each page starts with a contextual header (greeting, title, primary action) rather than navigation.
- **One `InspectionCard`** renders every inspection list. Adapters per API type (`inspectionCardModels.ts`) keep it presentational, and they only fill in data the API returns.
- **Notifications:** the API stores subjects in English, so the bell shows a title translated from the stable `type`.

**Consequences.**
- A new page should only need tokens and primitives. A raw colour class in a page is a smell to fix during review.
- Marketplace search, filters and sorting run on the client over the existing list. If the list grows large, it will need server-side paging and filters.
- Notifications have no "read" endpoint, so the bell marks recent activity (last three days) instead of an unread count.
