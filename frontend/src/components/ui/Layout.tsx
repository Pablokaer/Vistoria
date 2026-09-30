"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { cx } from "@/lib/cx";

/**
 * Content panel. Use for a group of related information — not around every element.
 * `flush` removes the inner padding for lists/tables that draw their own rows.
 */
export function Card({ title, actions, children, className, flush, description }:
  { title?: ReactNode; actions?: ReactNode; children: ReactNode; className?: string; flush?: boolean; description?: ReactNode }) {
  return (
    <section className={cx("rounded-lg border border-line bg-surface shadow-card", !flush && "p-4 sm:p-5", className)}>
      {(title || actions) && (
        <div className={cx("flex flex-wrap items-start justify-between gap-2", flush ? "border-b border-line px-4 py-3 sm:px-5" : "mb-4")}>
          <div className="min-w-0">
            {title && <h2 className="text-section font-semibold text-ink">{title}</h2>}
            {description && <p className="mt-0.5 text-label text-ink-3">{description}</p>}
          </div>
          {actions && <div className="flex shrink-0 flex-wrap items-center gap-2">{actions}</div>}
        </div>
      )}
      {children}
    </section>
  );
}

/**
 * Top of every page: optional eyebrow (context), title, subtitle and the page's actions (primary action last).
 * Example: `<PageHeader eyebrow="Good morning, Alex" title="Overview" actions={<LinkButton …/>} />`
 */
export function PageHeader({ title, subtitle, actions, back, eyebrow, meta }:
  { title: ReactNode; subtitle?: ReactNode; actions?: ReactNode; back?: { href: string; label: string }; eyebrow?: ReactNode; meta?: ReactNode }) {
  return (
    <header className="mb-6">
      {back && (
        <Link href={back.href} className="mb-3 inline-flex items-center gap-1 text-label font-medium text-ink-3 hover:text-brand">
          <Icon name="arrowLeft" className="h-4 w-4" />{back.label}
        </Link>
      )}
      <div className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
        <div className="min-w-0">
          {eyebrow && <p className="mb-1 text-label font-medium text-ink-3">{eyebrow}</p>}
          <h1 className="text-page font-semibold tracking-tight text-ink">{title}</h1>
          {subtitle && <p className="mt-1 max-w-2xl text-body text-ink-3">{subtitle}</p>}
          {meta && <div className="mt-2 flex flex-wrap items-center gap-2">{meta}</div>}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </header>
  );
}

/** Heading of a section inside a page, with an optional "see all" style link or actions. */
export function SectionHeader({ title, count, action, className }: { title: ReactNode; count?: number; action?: ReactNode; className?: string }) {
  return (
    <div className={cx("mb-3 flex items-center justify-between gap-3", className)}>
      <h2 className="flex items-center gap-2 text-section font-semibold text-ink">
        {title}
        {count !== undefined && <span className="tabular rounded-sm bg-neutral-50 px-1.5 text-caption font-medium text-ink-3">{count}</span>}
      </h2>
      {action}
    </div>
  );
}

/** "See all →" link used in section headers. */
export function SectionLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link href={href} className="inline-flex items-center gap-1 text-label font-medium text-brand hover:text-brand-dark">
      {children}<Icon name="chevronRight" className="h-4 w-4" />
    </Link>
  );
}

type MetricTone = "brand" | "info" | "warning" | "success" | "review" | "neutral";
const METRIC_TONES: Record<MetricTone, string> = {
  brand: "bg-brand-50 text-brand", info: "bg-info-50 text-info-700", warning: "bg-warning-50 text-warning-700",
  success: "bg-success-50 text-success-700", review: "bg-review-50 text-review-700", neutral: "bg-neutral-50 text-neutral-700",
};

/**
 * Compact figure with icon, label and optional context line; links to the list it counts.
 * Example: `<MetricCard icon="globe" label="Available" value={3} hint="in the marketplace" href="/agent/available" />`
 */
export function MetricCard({ icon, label, value, hint, href, tone = "brand", emphasis }:
  { icon: IconName; label: string; value: number | string; hint?: ReactNode; href?: string; tone?: MetricTone; emphasis?: boolean }) {
  const body = (
    <div className={cx("group flex h-full items-start gap-3 rounded-lg border bg-surface p-3.5 shadow-card transition",
      emphasis ? "border-brand/30 ring-1 ring-brand/10" : "border-line", href && "hover:border-line-strong hover:shadow-raised")}>
      <span className={cx("grid h-9 w-9 shrink-0 place-items-center rounded-md", METRIC_TONES[tone])}><Icon name={icon} className="h-5 w-5" /></span>
      <div className="min-w-0">
        <div className="tabular text-metric font-semibold text-ink">{value}</div>
        <div className="text-label font-medium text-ink-2">{label}</div>
        {hint && <div className="mt-0.5 line-clamp-2 text-caption text-ink-3">{hint}</div>}
      </div>
      {href && <Icon name="chevronRight" className="ml-auto h-4 w-4 shrink-0 self-center text-ink-4 transition group-hover:translate-x-0.5 group-hover:text-ink-3" />}
    </div>
  );
  return href ? <Link href={href} className="block rounded-lg">{body}</Link> : body;
}

/** Kept for existing call sites. Prefer MetricCard. */
export function Stat({ label, value, href }: { label: string; value: number | string; href?: string }) {
  return <MetricCard icon="layers" label={label} value={value} href={href} tone="neutral" />;
}

export function DefinitionList({ items, columns = 1 }: { items: [string, ReactNode][]; columns?: 1 | 2 }) {
  return (
    <dl className={cx("grid gap-x-8 gap-y-3 text-body", columns === 2 ? "sm:grid-cols-2" : "")}>
      {items.map(([k, v]) => (
        <div key={k} className="min-w-0">
          <dt className="text-caption font-medium uppercase tracking-wide text-ink-3">{k}</dt>
          <dd className="mt-0.5 font-medium text-ink">{v}</dd>
        </div>
      ))}
    </dl>
  );
}

/**
 * Segmented tabs that filter one list (client-side). Example:
 * `<Tabs value={tab} onChange={setTab} items={[{ value: "all", label: "All", count: 4 }]} />`
 */
export function Tabs<T extends string>({ value, onChange, items, label }:
  { value: T; onChange: (value: T) => void; items: { value: T; label: string; count?: number }[]; label: string }) {
  return (
    <div role="tablist" aria-label={label} className="-mx-1 flex gap-1 overflow-x-auto border-b border-line px-1">
      {items.map((item) => {
        const active = item.value === value;
        return (
          <button key={item.value} role="tab" aria-selected={active} onClick={() => onChange(item.value)}
            className={cx("-mb-px flex items-center gap-2 whitespace-nowrap border-b-2 px-3 py-2.5 text-body font-medium transition",
              active ? "border-brand text-ink" : "border-transparent text-ink-3 hover:text-ink")}>
            {item.label}
            {item.count !== undefined && (
              <span className={cx("tabular rounded-sm px-1.5 text-caption", active ? "bg-brand-50 text-brand" : "bg-neutral-50 text-ink-3")}>{item.count}</span>
            )}
          </button>
        );
      })}
    </div>
  );
}
