"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { Card, EmptyState, InspectionTypeTag, LinkButton, ProgressRing, Skeleton, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import type { InspectionSummary } from "@/lib/types";

function InspectionRow({ item }: { item: InspectionSummary }) {
  const t = useT(companyPropertyMessages);
  const { formatDate } = useFormatters();
  return (
    <li>
      <Link href={`/company/inspections/${item.id}`} className="group flex items-center gap-3 px-4 py-3 transition hover:bg-surface-2 sm:px-5">
        <ProgressRing value={item.roomsCompleted} total={item.roomsTotal} size={42} />
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2"><InspectionTypeTag type={item.inspectionType} /><StatusBadge value={item.status} /></div>
          <p className="mt-1 truncate text-label text-ink-3">
            {item.agentName ?? t("unassigned")} · {t("updated", { date: formatDate(item.completedAt ?? item.updatedAt) })}
          </p>
        </div>
        <Icon name="chevronRight" className="h-4 w-4 shrink-0 text-ink-4 transition group-hover:translate-x-0.5" />
      </Link>
    </li>
  );
}

/** Inspections of one property, newest first as returned by the API, each with its room progress. */
export function PropertyInspectionsPanel({ propertyId, items, loading }: { propertyId: string; items: InspectionSummary[] | null; loading: boolean }) {
  const t = useT(companyPropertyMessages);
  const newHref = `/company/inspections/new?propertyId=${propertyId}`;
  return (
    <Card flush title={t("inspections")} actions={<LinkButton variant="ghost" size="sm" icon="plus" href={newHref}>{t("newInspection")}</LinkButton>}>
      {loading && !items && <div className="space-y-2 p-4 sm:p-5"><Skeleton className="h-12" /><Skeleton className="h-12" /></div>}
      {items?.length === 0 && (
        <div className="p-4 sm:p-5"><EmptyState icon="clipboard" title={t("noInspections")}>{t("noInspectionsHint")}</EmptyState></div>
      )}
      {!!items?.length && <ul className="divide-y divide-line">{items.map((i) => <InspectionRow key={i.id} item={i} />)}</ul>}
    </Card>
  );
}
