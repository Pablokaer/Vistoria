"use client";

import type { ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";
import { cx } from "@/lib/cx";

export function Spinner({ small }: { small?: boolean }) {
  const t = useT(uiMessages);
  return <span role="status" className={cx("inline-block animate-spin rounded-full border-2 border-current border-t-transparent", small ? "h-4 w-4" : "h-5 w-5")} aria-label={t("loading")} />;
}

/** Inline loading line. Prefer a Skeleton shaped like the content when the layout is known. */
export function Loading({ label }: { label?: string }) {
  const t = useT(uiMessages);
  return <div className="flex items-center gap-3 py-8 text-body text-ink-3"><Spinner /> {label ?? t("loading")}</div>;
}

/** Placeholder block while data loads. Example: `<Skeleton className="h-24" />` */
export function Skeleton({ className }: { className?: string }) {
  return <div aria-hidden="true" className={cx("animate-shimmer rounded-md bg-neutral-50", className)} />;
}

/** Page-shaped skeleton: header line, a metrics row and two content blocks. */
export function PageSkeleton() {
  return (
    <div className="space-y-6" aria-busy="true">
      <Skeleton className="h-8 w-64" />
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">{[0, 1, 2, 3].map((i) => <Skeleton key={i} className="h-20" />)}</div>
      <Skeleton className="h-40" />
      <Skeleton className="h-28" />
    </div>
  );
}

export function ErrorBanner({ message, onRetry }: { message: string | null | undefined; onRetry?: () => void }) {
  const t = useT(uiMessages);
  if (!message) return null;
  return (
    <div role="alert" className="mb-4 flex flex-wrap items-center gap-3 rounded-lg border border-danger-500/30 bg-danger-50 px-4 py-3 text-body text-danger-700 animate-fade-in">
      <Icon name="alert" className="h-5 w-5 shrink-0" />
      <span className="flex-1">{message}</span>
      {onRetry && <button onClick={onRetry} className="font-medium underline underline-offset-2">{t("retry")}</button>}
    </div>
  );
}

type NoticeTone = "info" | "success" | "warning";
const NOTICE: Record<NoticeTone, { className: string; icon: IconName }> = {
  info: { className: "border-info-500/25 bg-info-50 text-info-700", icon: "info" },
  success: { className: "border-success-500/30 bg-success-50 text-success-700", icon: "checkCircle" },
  warning: { className: "border-warning-500/30 bg-warning-50 text-warning-700", icon: "alert" },
};

/** Inline message inside the page flow (not a toast). */
export function Notice({ tone = "info", children }: { tone?: NoticeTone; children: ReactNode }) {
  const { className, icon } = NOTICE[tone];
  return (
    <div className={cx("mb-4 flex gap-3 rounded-lg border px-4 py-3 text-body", className)}>
      <Icon name={icon} className="mt-0.5 h-4.5 w-4.5 shrink-0" />
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  );
}

/**
 * Compact empty state: icon, title, one line of explanation and at most one action. Never a page-sized box.
 * Example: `<EmptyState icon="inbox" title="No active inspections" action={<LinkButton …/>}>You're all caught up.</EmptyState>`
 */
export function EmptyState({ title, children, icon = "inbox", action, className }:
  { title: string; children?: ReactNode; icon?: IconName; action?: ReactNode; className?: string }) {
  return (
    <div className={cx("flex flex-col items-start gap-3 rounded-lg border border-dashed border-line-strong bg-surface px-5 py-5 sm:flex-row sm:items-center", className)}>
      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand"><Icon name={icon} className="h-5 w-5" /></span>
      <div className="min-w-0 flex-1">
        <p className="text-card-title font-semibold text-ink">{title}</p>
        {children && <div className="mt-0.5 text-body text-ink-3">{children}</div>}
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  );
}
