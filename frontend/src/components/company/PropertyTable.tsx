"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { Skeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { cx } from "@/lib/cx";
import type { PropertySummary } from "@/lib/types";

// Desktop columns: property | type | rooms | tenancies | open inspections | chevron.
const GRID = "md:grid md:grid-cols-[minmax(0,2.4fr)_minmax(0,1fr)_5rem_7rem_8rem_1.25rem] md:items-center md:gap-4";

function Count({ value, label, highlight }: { value: number; label: string; highlight?: boolean }) {
  return (
    <span className={cx("tabular inline-flex items-center gap-1 text-label md:justify-end", highlight ? "font-semibold text-brand" : "text-ink-2")}>
      {value}<span className="text-ink-3 md:sr-only">{label}</span>
    </span>
  );
}

function PropertyRow({ property: p }: { property: PropertySummary }) {
  const t = useT(companyPageMessages);
  const { humanize } = useFormatters();
  return (
    <li>
      <Link href={`/company/properties/${p.id}`} className={cx("group block px-4 py-3 transition hover:bg-surface-2 sm:px-5", GRID)}>
        <div className="flex min-w-0 items-center gap-3">
          <span aria-hidden="true" className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-brand-50 text-brand"><Icon name="building" className="h-5 w-5" /></span>
          <span className="min-w-0">
            <span className="block truncate text-card-title font-medium text-ink">{p.addressLine1}{p.addressLine2 && `, ${p.addressLine2}`}</span>
            <span className="block truncate text-label text-ink-3">{p.city} {p.postcode}</span>
          </span>
        </div>
        <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 pl-12 md:contents">
          <span className="text-label text-ink-2">{humanize(p.propertyType)}</span>
          <Count value={p.roomCount} label={t("colRooms")} />
          <Count value={p.activeTenancies} label={t("colTenancies")} />
          <Count value={p.openInspections} label={t("colInspections")} highlight={p.openInspections > 0} />
          <Icon name="chevronRight" className="hidden h-4 w-4 text-ink-4 transition group-hover:translate-x-0.5 md:block" />
        </div>
      </Link>
    </li>
  );
}

/** Dense list of properties; a real table header on desktop, stacked facts on phones. */
export function PropertyTable({ items }: { items: PropertySummary[] }) {
  const t = useT(companyPageMessages);
  return (
    <div>
      <div aria-hidden="true" className={cx("hidden border-b border-line bg-surface-2 px-5 py-2 text-caption font-medium uppercase tracking-wide text-ink-3", GRID)}>
        <span>{t("colProperty")}</span><span>{t("colType")}</span>
        <span className="text-right">{t("colRooms")}</span><span className="text-right">{t("colTenancies")}</span>
        <span className="text-right">{t("colInspections")}</span><span />
      </div>
      <ul className="divide-y divide-line">{items.map((p) => <PropertyRow key={p.id} property={p} />)}</ul>
    </div>
  );
}

export function PropertyTableSkeleton() {
  return <div className="space-y-px p-4 sm:p-5" aria-busy="true">{[0, 1, 2, 3].map((i) => <Skeleton key={i} className="mb-3 h-11" />)}</div>;
}
