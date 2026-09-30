"use client";

import { Icon, type IconName } from "@/components/icons";
import { useFormatters } from "@/i18n/I18nProvider";
import { cx } from "@/lib/cx";
import type { InspectionType, Visibility } from "@/lib/types";

type Tone = "neutral" | "info" | "progress" | "review" | "warning" | "success" | "danger" | "muted";

const TONES: Record<Tone, string> = {
  neutral: "bg-neutral-50 text-neutral-700 ring-neutral-700/15",
  info: "bg-info-50 text-info-700 ring-info-700/15",
  progress: "bg-brand-50 text-brand ring-brand/20",
  review: "bg-review-50 text-review-700 ring-review-700/15",
  warning: "bg-warning-50 text-warning-700 ring-warning-700/20",
  success: "bg-success-50 text-success-700 ring-success-700/15",
  danger: "bg-danger-50 text-danger-700 ring-danger-700/15",
  muted: "bg-neutral-50 text-ink-3 ring-ink-3/15",
};

// Every status the API can return gets one tone + one icon, used identically on every page.
const STATUS: Record<string, { tone: Tone; icon: IconName }> = {
  Draft: { tone: "neutral", icon: "edit" },
  Open: { tone: "info", icon: "globe" },
  Available: { tone: "info", icon: "globe" },
  Assigned: { tone: "progress", icon: "userCheck" },
  InProgress: { tone: "warning", icon: "play" },
  Review: { tone: "review", icon: "eye" },
  Completed: { tone: "success", icon: "checkCircle" },
  AwaitingTenant: { tone: "warning", icon: "clock" },
  Accepted: { tone: "success", icon: "checkCircle" },
  Disputed: { tone: "danger", icon: "flag" },
  Cancelled: { tone: "muted", icon: "xCircle" },
  Expired: { tone: "muted", icon: "clock" },
  Pending: { tone: "neutral", icon: "circleDashed" },
  Processing: { tone: "progress", icon: "sparkles" },
  Failed: { tone: "danger", icon: "alert" },
  NewDamage: { tone: "danger", icon: "alert" },
  PreExisting: { tone: "neutral", icon: "history" },
  NormalWear: { tone: "warning", icon: "clock" },
  Unchanged: { tone: "success", icon: "check" },
  Resolved: { tone: "info", icon: "checkCircle" },
  UnableToDetermine: { tone: "muted", icon: "help" },
  Unknown: { tone: "muted", icon: "help" },
  Active: { tone: "success", icon: "checkCircle" },
  PastDue: { tone: "warning", icon: "alert" },
  Upcoming: { tone: "info", icon: "calendar" },
  Ended: { tone: "muted", icon: "clock" },
};

/**
 * Status pill with icon + label (never colour alone). Accepts any API status/classification value.
 * Example: `<StatusBadge value="InProgress" />`
 */
export function StatusBadge({ value, className, label }: { value: string; className?: string; label?: string }) {
  const { humanize } = useFormatters();
  const { tone, icon } = STATUS[value] ?? { tone: "neutral" as Tone, icon: "circleDashed" as IconName };
  return (
    <span className={cx("inline-flex items-center gap-1 whitespace-nowrap rounded-sm px-2 py-0.5 text-caption font-medium ring-1 ring-inset", TONES[tone], className)}>
      <Icon name={icon} className="h-3.5 w-3.5" />
      {label ?? humanize(value)}
    </span>
  );
}

/** Kept for existing call sites: same as StatusBadge. */
export const Badge = StatusBadge;

const TYPE: Record<InspectionType, { icon: IconName; className: string }> = {
  MoveIn: { icon: "doorIn", className: "bg-movein-50 text-movein" },
  MoveOut: { icon: "doorOut", className: "bg-moveout-50 text-moveout" },
  Periodic: { icon: "calendar", className: "bg-periodic-50 text-periodic" },
  Other: { icon: "clipboard", className: "bg-neutral-50 text-neutral-700" },
};

/** Inspection type identity: Move In (teal, door in), Move Out (orange, door out), Periodic (indigo, calendar). */
export function InspectionTypeTag({ type, className }: { type: InspectionType; className?: string }) {
  const { humanize } = useFormatters();
  const { icon, className: tone } = TYPE[type] ?? TYPE.Other;
  return (
    <span className={cx("inline-flex items-center gap-1 whitespace-nowrap rounded-sm px-1.5 py-0.5 text-caption font-semibold", tone, className)}>
      <Icon name={icon} className="h-3.5 w-3.5" />
      {humanize(type)}
    </span>
  );
}

/** Square property thumbnail tinted by inspection type — a consistent placeholder where no photo exists. */
export function PropertyThumb({ type, size = "md" }: { type?: InspectionType; size?: "sm" | "md" | "lg" }) {
  const { className } = TYPE[type ?? "Other"] ?? TYPE.Other;
  const box = { sm: "h-9 w-9", md: "h-11 w-11", lg: "h-14 w-14" }[size];
  return (
    <span aria-hidden="true" className={cx("grid shrink-0 place-items-center rounded-lg", box, className)}>
      <Icon name="building" className={size === "lg" ? "h-7 w-7" : "h-5 w-5"} />
    </span>
  );
}

/** Public (marketplace) vs Private (link + code). */
export function VisibilityTag({ visibility }: { visibility: Visibility }) {
  const { humanize } = useFormatters();
  return (
    <span className="inline-flex items-center gap-1 text-caption font-medium text-ink-3">
      <Icon name={visibility === "Private" ? "lock" : "globe"} className="h-3.5 w-3.5" />
      {humanize(visibility)}
    </span>
  );
}
