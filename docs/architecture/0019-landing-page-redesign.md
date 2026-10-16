# 0019 — Landing page redesign: architectural photography and one blue accent

**Context.**

- The "paper record" landing page of ADR 0018 (warm paper surface, Newsreader serif, navy actions, drawn room sketches) read as an editorial document. The product needed a public page that feels like a premium property-technology product for paying letting agencies: bright, photographic, with a clear path for both companies and inspectors.
- The brief set the direction: white and cool-grey surfaces, one brand blue (`#1769E0`), a refined sans-serif, architectural photography, a real-looking product visualization, restrained motion, and no invented features, prices, testimonials or contact details.
- The routing of "/" (session hint cookie, `SignedInRedirect`) from ADR 0018 is unaffected and kept as is.

**Decision.**

- **Tokens.** Public pages get their own `site-*` tokens in `globals.css` (`site-blue`, `site-blue-soft`, `site-ink`, `site-muted`, `site-mist`, `site-line`, `site-night` and two shadows). The app's tokens are unchanged. One adaptation of the brief: secondary text is `#5B6B80` instead of `#64748B`, because `#64748B` only reaches 4.47:1 on the `#F6F8FB` background and `#5B6B80` passes AA (5.1:1) on both surfaces.
- **Typography.** Inter only, the font the app already loads through `next/font`. The Newsreader serif and its font file are removed, so the public pages load one font family fewer. Headlines use weight 600 with tight tracking. The hero headline has deliberate line breaks, and the promise ("made easy." / "sem complicação.") is always on its own line.
- **Photography.** Four Unsplash photos (Unsplash License, none Unsplash+) are kept as local, resized copies in `frontend/src/assets/landing`, with credits in `CREDITS.md` there. They are imported statically and rendered with `next/image`, which provides intrinsic sizes (no layout shift), blur placeholders, responsive `sizes`, WebP and `preload` for the hero image. No image is loaded from a third party at runtime.
- **Sections.**
  - Sticky header: gains a hairline and a shadow on scroll through `useScrollFlag`, a data attribute, so there are no re-renders. Below 1024px it becomes a disclosure menu with 48px rows and Escape to close.
  - Hero: the interior photo fades into the text column through a CSS mask. A room status card reuses the app's success badge.
  - Four feature cards: each one ends with the concrete mechanism behind its claim.
  - AI section: a phone mockup of the inspector's *Descriptions* step, drawn with the app's own labels (step names, "Take photo", the "AI draft · review and edit" chip and its violet tint), in front of a photo of the same room.
  - A four-stage "How it works": a connected line, horizontal on desktop and vertical on phones.
  - "For agents": links to `/register?role=Agent`.
  - Pricing: plans come from the API as before.
  - Final call to action: on a dusk photo.
  - Footer.
- **Honesty rules.**
  - The AI copy says what the implementation does. The AI describes walls, ceiling, floor and visible items from photos, and every draft is labelled, editable and reviewed by the inspector.
  - Prices come only from `GET /api/billing/plans`.
  - There is no Contact link: the product has no contact address or page, and no legal pages to link either.
- **Motion.**
  - `Reveal` hides a block only when it starts below the fold, after hydration, and never with `prefers-reduced-motion`. It reveals the block once with a 700 ms opacity and translate transition.
  - Hover states raise the cards by 2px.
  - No animation library.

**Consequences.**

- The landing bundle stays lean. Every section except the header, the pricing list and the two small motion helpers is a server component.
- The public pages no longer share a visual language with the PDF report specimen, which has been removed. The app's own screens are untouched.
- Swapping a photo means replacing the file in `src/assets/landing` with the same name, or updating `landingPhotos.ts`. Credits live next to the files.
- `e2e/landing.mjs` covers:
  - Every call to action: Sign in, Get started, See how it works, Join as an agent, Get started today.
  - Layouts at 375, 430, 768, 1024, 1440 and 1920 px with no horizontal scroll.
  - axe WCAG 2.2 AA on the fully revealed page.
- `e2e/subscription.mjs` checks that Escape closes the mobile menu.
