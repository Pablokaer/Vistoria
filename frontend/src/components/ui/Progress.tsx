"use client";

import { useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";
import { cx } from "@/lib/cx";

const percent = (value: number, total: number) => (total === 0 ? 0 : Math.round((value / total) * 100));

/** Labelled progress bar. Example: `<ProgressBar value={3} total={6} />` → "3 / 6 rooms completed · 50%". */
export function ProgressBar({ value, total, label, compact }: { value: number; total: number; label?: string; compact?: boolean }) {
  const t = useT(uiMessages);
  const pct = percent(value, total);
  const done = total > 0 && value >= total;
  return (
    <div>
      {!compact && (
        <div className="mb-1.5 flex justify-between gap-2 text-caption text-ink-3">
          <span>{label ?? t("roomsCompleted", { value, total })}</span><span className="tabular font-medium text-ink-2">{pct}%</span>
        </div>
      )}
      <div role="progressbar" aria-valuemin={0} aria-valuemax={total} aria-valuenow={value} aria-label={label ?? t("roomsCompleted", { value, total })}
        className="h-1.5 overflow-hidden rounded-full bg-neutral-50 ring-1 ring-inset ring-line">
        <div className={cx("h-full rounded-full transition-[width] duration-500 ease-out", done ? "bg-success-500" : "bg-brand")} style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

/** Small circular progress for dense rows (room lists, cards). */
export function ProgressRing({ value, total, size = 42 }: { value: number; total: number; size?: number }) {
  const t = useT(uiMessages);
  const pct = percent(value, total);
  const r = (size - 5) / 2;
  const c = 2 * Math.PI * r;
  return (
    <span className="relative inline-grid shrink-0 place-items-center" style={{ width: size, height: size }}
      role="img" aria-label={t("roomsCompleted", { value, total })}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" strokeWidth={3.5} className="stroke-line" />
        <circle cx={size / 2} cy={size / 2} r={r} fill="none" strokeWidth={3.5} strokeLinecap="round"
          className={cx("transition-[stroke-dashoffset] duration-500", pct === 100 ? "stroke-success-500" : "stroke-brand")}
          strokeDasharray={c} strokeDashoffset={c - (c * pct) / 100} />
      </svg>
      <span className="tabular absolute text-[9px] font-semibold text-ink-2">{pct}%</span>
    </span>
  );
}
