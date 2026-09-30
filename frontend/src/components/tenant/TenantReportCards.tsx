"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { InspectionTypeTag, PropertyThumb, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";
import type { TenantInspection } from "@/lib/types";

const reviewHref = (i: TenantInspection) => `/tenant/inspections/${i.id}`;

/**
 * A report waiting for the tenant: the whole card is the link (its name includes the inspection type),
 * with one explicit call to action.
 */
export function AwaitingReviewCard({ item }: { item: TenantInspection }) {
  const t = useT(tenantMessages);
  const { formatDate } = useFormatters();
  return (
    <Link href={reviewHref(item)} className="group flex flex-col gap-4 rounded-lg border border-brand/25 bg-surface p-5 shadow-card ring-1 ring-brand/10 transition hover:border-brand/40 hover:shadow-raised sm:flex-row sm:items-center">
      <PropertyThumb type={item.inspectionType} size="lg" />
      <div className="min-w-0 flex-1">
        <div className="mb-1 flex flex-wrap items-center gap-2"><InspectionTypeTag type={item.inspectionType} /><StatusBadge value={item.status} /></div>
        <p className="text-card-title font-semibold text-ink">{item.propertyAddress}</p>
        <p className="text-label text-ink-3">
          {item.companyName}
          {item.reportNumber && <> · {t("reportNumberLabel", { number: item.reportNumber })}</>}
          {item.sentToTenantAt && <> · {t("sentOn", { date: formatDate(item.sentToTenantAt) })}</>}
        </p>
      </div>
      <span className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-brand px-4 text-body font-semibold text-white transition group-hover:bg-brand-dark">
        {t("reviewCta")}<Icon name="arrowRight" className="h-4 w-4" />
      </span>
    </Link>
  );
}

/** Accepted and disputed reports in one quiet list, newest response first. */
export function TenantHistory({ items }: { items: TenantInspection[] }) {
  const t = useT(tenantMessages);
  const { formatDate, humanize } = useFormatters();
  if (items.length === 0) return <p className="text-label text-ink-3">{t("noHistory")}</p>;
  return (
    <ul className="divide-y divide-line overflow-hidden rounded-lg border border-line bg-surface shadow-card">
      {items.map((i) => (
        <li key={i.id}>
          <Link href={reviewHref(i)} className="flex items-center gap-3 px-4 py-3 transition hover:bg-surface-2">
            <PropertyThumb type={i.inspectionType} size="sm" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-body font-medium text-ink">{humanize(i.inspectionType)} · {i.propertyAddress}</p>
              <p className="truncate text-caption text-ink-3">{i.companyName}{i.respondedAt && ` · ${t("respondedOn", { date: formatDate(i.respondedAt) })}`}</p>
            </div>
            <StatusBadge value={i.status} />
            <Icon name="chevronRight" className="h-4 w-4 shrink-0 text-ink-4" />
          </Link>
        </li>
      ))}
    </ul>
  );
}
