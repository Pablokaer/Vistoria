"use client";

import type { ReactNode } from "react";
import { cx } from "@/lib/cx";

/**
 * One step of a long form: number + title + short explanation on the left (desktop), the fields in a panel on the
 * right. Steps make "Add property" and "New inspection" read top-to-bottom instead of as a wall of inputs.
 * Example: `<FormSection step={1} title="Address" description="Where the property is">…fields…</FormSection>`
 */
export function FormSection({ step, title, description, children, aside, muted }:
  { step: number; title: string; description?: string; children: ReactNode; aside?: ReactNode; muted?: boolean }) {
  return (
    <section className="grid grid-cols-1 gap-3 md:grid-cols-[13rem_minmax(0,1fr)] md:gap-6">
      <div className="flex items-start gap-3 md:block">
        <span aria-hidden="true" className={cx("tabular grid h-7 w-7 shrink-0 place-items-center rounded-full text-caption font-semibold md:mb-2",
          muted ? "bg-neutral-50 text-ink-3" : "bg-brand-50 text-brand")}>{step}</span>
        <div>
          <h2 className="text-section font-semibold text-ink">{title}</h2>
          {description && <p className="mt-0.5 text-label text-ink-3">{description}</p>}
          {aside && <div className="mt-2">{aside}</div>}
        </div>
      </div>
      <div className={cx("rounded-lg border border-line bg-surface p-4 shadow-card transition sm:p-5", muted && "opacity-60")}>{children}</div>
    </section>
  );
}

/**
 * Submit area that stays reachable while scrolling a long form: sticky above the phone tab bar, inline on desktop.
 * `summary` explains what the button will do (e.g. "Creates the property and publishes the inspection").
 */
export function StickyFormActions({ summary, children }: { summary?: ReactNode; children: ReactNode }) {
  return (
    <div className="sticky bottom-16 z-20 -mx-4 border-t border-line bg-surface/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6 lg:static lg:mx-0 lg:rounded-lg lg:border lg:bg-surface lg:px-5 lg:shadow-card">
      <div className="flex flex-wrap items-center justify-end gap-3">
        {summary && <p className="mr-auto hidden text-label text-ink-3 sm:block">{summary}</p>}
        {children}
      </div>
    </div>
  );
}
