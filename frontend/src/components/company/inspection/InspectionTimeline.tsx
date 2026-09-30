"use client";

import { Icon } from "@/components/icons";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import { cx } from "@/lib/cx";
import type { InspectionDetails } from "@/lib/types";

type StepKey = "published" | "accepted" | "started" | "finalized" | "sentToTenant" | "tenantResponded";

/** The life-cycle steps with the real timestamp of each (null = not reached yet). */
function steps(i: InspectionDetails): { key: StepKey; at: string | null }[] {
  const all: { key: StepKey; at: string | null }[] = [
    { key: "published", at: i.publishedAt }, { key: "accepted", at: i.acceptedAt }, { key: "started", at: i.startedAt },
    { key: "finalized", at: i.completedAt }, { key: "sentToTenant", at: i.sentToTenantAt }, { key: "tenantResponded", at: i.tenantRespondedAt },
  ];
  // Without tenants nothing is sent, so the tenant steps only appear once they can happen.
  return i.tenancy?.members.length || i.sentToTenantAt ? all : all.slice(0, 4);
}

/**
 * Horizontal stepper on desktop, vertical on phones: done steps show their date, the first open step is "current".
 * Cancelled inspections stop the line (no step is current).
 */
export function InspectionTimeline({ inspection }: { inspection: InspectionDetails }) {
  const t = useT(companyInspectionMessages);
  const { formatDateTime } = useFormatters();
  const list = steps(inspection);
  const stopped = inspection.status === "Cancelled" || inspection.status === "Expired";
  const current = stopped ? -1 : list.findIndex((s) => !s.at);
  return (
    <ol aria-label={t("timeline")} className="grid grid-cols-1 gap-3 rounded-lg border border-line bg-surface p-4 shadow-card sm:p-5 md:grid-cols-none md:grid-flow-col md:auto-cols-fr md:gap-0">
      {list.map((s, index) => {
        const done = !!s.at;
        const isCurrent = index === current;
        return (
          <li key={s.key} aria-current={isCurrent ? "step" : undefined} className="relative flex gap-3 md:flex-col md:gap-2 md:pr-3">
            {index < list.length - 1 && <span aria-hidden="true" className={cx("absolute hidden h-0.5 md:left-7 md:right-0 md:top-3 md:block", done ? "bg-success-500" : "bg-line")} />}
            <span className={cx("relative z-10 grid h-6 w-6 shrink-0 place-items-center rounded-full ring-4 ring-surface",
              done ? "bg-success-500 text-white" : isCurrent ? "bg-brand text-white" : "bg-neutral-50 text-ink-4 ring-1 ring-line")}>
              <Icon name={done ? "check" : isCurrent ? "play" : "circleDashed"} className="h-3.5 w-3.5" />
            </span>
            <span className="min-w-0">
              <span className={cx("block text-label font-medium", done || isCurrent ? "text-ink" : "text-ink-3")}>{t(s.key)}</span>
              <span className="block text-caption text-ink-3">{done ? formatDateTime(s.at) : isCurrent ? t("stepCurrent") : t("stepNotYet")}</span>
            </span>
          </li>
        );
      })}
    </ol>
  );
}
