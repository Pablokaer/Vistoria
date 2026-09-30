"use client";

import Link from "next/link";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { inspectionListMessages } from "@/i18n/messages/inspection-lists";
import type { AvailableInspection, InspectionSummary } from "@/lib/types";
import { Badge, EmptyState, ProgressBar } from "./ui";

export function AvailableCard({ item }: { item: AvailableInspection }) {
  const t = useT(inspectionListMessages);
  const { formatDate, humanize } = useFormatters();
  return (
    <Link href={`/agent/available/${item.id}`} className="block rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand/40">
      <div className="flex items-start justify-between gap-2">
        <div>
          <div className="font-medium text-slate-900">{humanize(item.inspectionType)} · {item.city} {item.postcodeArea}</div>
          <div className="text-sm text-slate-600">{item.companyName}</div>
        </div>
        {item.hasComparison && <span className="rounded-full bg-violet-100 px-2 py-0.5 text-xs text-violet-800">{t("withBaseline")}</span>}
      </div>
      <div className="mt-3 flex flex-wrap gap-3 text-xs text-slate-500">
        <span>{humanize(item.propertyType)}</span><span>{t("rooms", { count: item.roomCount })}</span>
        {item.scheduledDate && <span>{t("scheduled", { date: formatDate(item.scheduledDate) })}</span>}
        {item.acceptBy && <span>{t("acceptBy", { date: formatDate(item.acceptBy) })}</span>}
      </div>
    </Link>
  );
}

export function MyInspectionList({ items, empty }: { items: InspectionSummary[]; empty: string }) {
  const { formatDate, humanize } = useFormatters();
  if (items.length === 0) return <EmptyState title={empty} />;
  return (
    <div className="grid gap-3 sm:grid-cols-2">
      {items.map((i) => (
        <Link key={i.id} href={`/agent/inspections/${i.id}`} className="block rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand/40">
          <div className="mb-2 flex items-start justify-between gap-2">
            <div>
              <div className="font-medium text-slate-900">{i.propertyAddress}</div>
              <div className="text-sm text-slate-600">{humanize(i.inspectionType)}{i.scheduledDate && ` · ${formatDate(i.scheduledDate)}`}</div>
            </div>
            <Badge value={i.status} />
          </div>
          <ProgressBar value={i.roomsCompleted} total={i.roomsTotal} />
        </Link>
      ))}
    </div>
  );
}
