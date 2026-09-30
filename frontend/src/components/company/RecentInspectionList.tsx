"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { InspectionTypeTag, PropertyThumb, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyDashboardMessages } from "@/i18n/messages/company-dashboard";
import type { CompanyDashboard } from "@/lib/types";

export type RecentInspection = CompanyDashboard["recentInspections"][number];

/** Statuses that mean a report exists (finalized at least once). */
export const REPORT_STATUSES = ["Completed", "AwaitingTenant", "Accepted", "Disputed"];

/**
 * Dense, scannable rows: property + type on the left, inspector and update time in the middle, status on the right.
 * On phones the middle column folds under the address.
 */
export function RecentInspectionList({ items }: { items: RecentInspection[] }) {
  return (
    <ul className="divide-y divide-line">
      {items.map((i) => <RecentInspectionRow key={i.id} item={i} />)}
    </ul>
  );
}

function RecentInspectionRow({ item }: { item: RecentInspection }) {
  const t = useT(companyDashboardMessages);
  const { formatDateTime } = useFormatters();
  return (
    <li>
      <Link href={`/company/inspections/${item.id}`}
        className="group grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1 px-4 py-3 transition hover:bg-surface-2 sm:px-5 md:grid-cols-[auto_minmax(0,2fr)_minmax(0,1.2fr)_auto]">
        <PropertyThumb type={item.inspectionType} size="sm" />
        <div className="min-w-0">
          <p className="truncate text-card-title font-medium text-ink">{item.propertyAddress}</p>
          <div className="mt-0.5 flex flex-wrap items-center gap-2">
            <InspectionTypeTag type={item.inspectionType} />
            <span className="text-caption text-ink-3 md:hidden">{item.agentName ?? t("noAgentYet")}</span>
          </div>
        </div>
        <div className="hidden min-w-0 text-label md:block">
          <p className="flex items-center gap-1.5 truncate text-ink-2"><Icon name="user" className="h-4 w-4 text-ink-4" />{item.agentName ?? t("noAgentYet")}</p>
          <p className="mt-0.5 text-caption text-ink-3">{t("updated", { date: formatDateTime(item.updatedAt) })}</p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge value={item.status} />
          <Icon name="chevronRight" className="hidden h-4 w-4 text-ink-4 transition group-hover:translate-x-0.5 sm:block" />
        </div>
      </Link>
    </li>
  );
}

/** Latest finalized reports (subset of recent inspections), as a compact side list. */
export function RecentReportList({ items }: { items: RecentInspection[] }) {
  const t = useT(companyDashboardMessages);
  const { formatDate } = useFormatters();
  if (items.length === 0) return <p className="px-4 py-4 text-label text-ink-3 sm:px-5">{t("noReportsYet")}</p>;
  return (
    <ul className="divide-y divide-line">
      {items.map((i) => (
        <li key={i.id}>
          <Link href={`/company/inspections/${i.id}`} className="flex items-center gap-3 px-4 py-2.5 transition hover:bg-surface-2 sm:px-5">
            <Icon name="file" className="h-5 w-5 shrink-0 text-ink-4" />
            <span className="min-w-0 flex-1">
              <span className="block truncate text-label font-medium text-ink">{i.propertyAddress}</span>
              <span className="block text-caption text-ink-3">{formatDate(i.updatedAt)}</span>
            </span>
            <StatusBadge value={i.status} />
          </Link>
        </li>
      ))}
    </ul>
  );
}
