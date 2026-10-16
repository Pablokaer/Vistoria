// Class names shared by the public pages, so the landing page and /pricing speak one visual language (ADR 0018).

/** Page width and gutters (16px on phones). */
export const LANDING_CONTAINER = "mx-auto w-full max-w-[72rem] px-4 sm:px-6 lg:px-8";

/** Primary action: solid navy, the colour of the app's sidebar. */
export const PRIMARY_LINK =
  "inline-flex min-h-12 items-center justify-center gap-2 rounded-md bg-sidebar px-5 text-[0.9375rem] font-semibold text-white transition hover:bg-sidebar-2 active:translate-y-px";

/** Secondary action: hairline outline on paper. */
export const SECONDARY_LINK =
  "inline-flex min-h-12 items-center justify-center gap-2 rounded-md border border-rule-strong px-5 text-[0.9375rem] font-medium text-ink transition hover:border-ink hover:bg-paper-2";

/** Small monospace label above section titles. */
export const EYEBROW = "font-mono text-[0.6875rem] font-medium uppercase tracking-[0.16em] text-paper-muted";

/** Serif section title. */
export const SECTION_TITLE = "font-display text-[clamp(2rem,3.4vw,2.875rem)] leading-[1.05] tracking-[-0.015em] text-ink";
