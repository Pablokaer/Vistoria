// Class names shared by the public pages, so the landing page and /pricing speak one visual language (ADR 0019).

/** Page width and gutters (16px on phones). */
export const LANDING_CONTAINER = "mx-auto w-full max-w-[76rem] px-4 sm:px-6 lg:px-8";

const ACTION =
  "inline-flex min-h-12 items-center justify-center gap-2 rounded-lg px-5 text-[0.9375rem] font-semibold transition duration-200 active:translate-y-px";

/** Primary action: the site blue. */
export const PRIMARY_LINK = `${ACTION} bg-site-blue text-white shadow-[0_1px_2px_rgb(16_24_40/0.14)] hover:bg-site-blue-dark`;

/** Secondary action: hairline outline on white. */
export const SECONDARY_LINK = `${ACTION} border border-site-line-strong bg-white text-site-ink hover:border-site-ink/30 hover:bg-site-mist`;

/** Small uppercase label above section titles. */
export const EYEBROW = "text-[0.75rem] font-semibold uppercase tracking-[0.14em] text-site-blue";

/** Section title: restrained weight, tight tracking, balanced line breaks. */
export const SECTION_TITLE =
  "text-balance text-[clamp(1.875rem,3.4vw,2.75rem)] font-semibold leading-[1.1] tracking-[-0.03em] text-site-ink";

/** Supporting paragraph under a section title, at a comfortable reading width. */
export const SECTION_INTRO = "max-w-[36rem] text-[1.0625rem] leading-relaxed text-site-muted";

/** Vertical rhythm shared by every content section. */
export const SECTION_SPACING = "py-20 sm:py-24 lg:py-32";
