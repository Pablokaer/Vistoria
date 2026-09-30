"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { InspectionCard } from "@/components/inspection/InspectionCard";
import { fromAvailable, fromSummary } from "@/components/inspection/inspectionCardModels";
import { PropertyThumb, SectionHeader, SectionLink, Skeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentDashboardMessages } from "@/i18n/messages/agent-dashboard";
import { inspectionListMessages } from "@/i18n/messages/inspection-lists";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection, InspectionSummary } from "@/lib/types";

const PREVIEW_COUNT = 3;

/** One quiet line instead of an empty box when a side list has nothing yet. */
function QuietEmpty({ children }: { children: string }) {
  return <p className="rounded-lg border border-dashed border-line-strong px-4 py-3 text-label text-ink-3">{children}</p>;
}

function ListSkeleton() {
  return <div className="space-y-2">{[0, 1, 2].map((i) => <Skeleton key={i} className="h-16" />)}</div>;
}

/** Newest marketplace opportunities; the full, filterable list is one click away. */
export function AvailablePreview() {
  const d = useT(agentDashboardMessages);
  const t = useT(inspectionListMessages);
  const formatters = useFormatters();
  const { data } = useApi<AvailableInspection[]>("/api/agent/available");
  const newest = [...(data ?? [])].sort((a, b) => (b.publishedAt ?? "").localeCompare(a.publishedAt ?? "")).slice(0, PREVIEW_COUNT);
  return (
    <section>
      <SectionHeader title={d("availableSection")} count={data?.length} action={<SectionLink href="/agent/available">{d("seeAll")}</SectionLink>} />
      {!data ? <ListSkeleton /> : newest.length === 0 ? <QuietEmpty>{d("noAvailable")}</QuietEmpty> : (
        <div className="grid gap-3">{newest.map((i) => <InspectionCard key={i.id} model={fromAvailable(i, t, formatters)} />)}</div>
      )}
    </section>
  );
}

function CompletedRow({ item }: { item: InspectionSummary }) {
  const d = useT(agentDashboardMessages);
  const { formatDate, humanize } = useFormatters();
  return (
    <li>
      <Link href={`/agent/inspections/${item.id}`} className="group flex items-center gap-3 px-4 py-3 transition hover:bg-surface-2">
        <PropertyThumb type={item.inspectionType} size="sm" />
        <div className="min-w-0 flex-1">
          <p className="truncate text-body font-medium text-ink">{item.propertyAddress}</p>
          <p className="truncate text-caption text-ink-3">{humanize(item.inspectionType)} · {formatDate(item.completedAt)}</p>
        </div>
        <span className="inline-flex items-center gap-1 text-label font-medium text-ink-3 group-hover:text-brand">{d("viewReport")}<Icon name="chevronRight" className="h-4 w-4" /></span>
      </Link>
    </li>
  );
}

/** The last finalized inspections, as a dense list (they are reference, not work). */
export function RecentlyCompleted() {
  const d = useT(agentDashboardMessages);
  const { data } = useApi<InspectionSummary[]>("/api/agent/inspections?scope=completed");
  const latest = [...(data ?? [])].sort((a, b) => (b.completedAt ?? "").localeCompare(a.completedAt ?? "")).slice(0, PREVIEW_COUNT);
  return (
    <section>
      <SectionHeader title={d("recentlyCompleted")} action={<SectionLink href="/agent/completed">{d("seeAll")}</SectionLink>} />
      {!data ? <ListSkeleton /> : latest.length === 0 ? <QuietEmpty>{d("noCompleted")}</QuietEmpty> : (
        <ul className="divide-y divide-line overflow-hidden rounded-lg border border-line bg-surface shadow-card">{latest.map((i) => <CompletedRow key={i.id} item={i} />)}</ul>
      )}
    </section>
  );
}

/** Other active inspections besides the spotlighted one. */
export function OtherActive({ items }: { items: InspectionSummary[] }) {
  const d = useT(agentDashboardMessages);
  const t = useT(inspectionListMessages);
  const formatters = useFormatters();
  if (items.length === 0) return null;
  return (
    <section>
      <SectionHeader title={d("alsoActive")} count={items.length} />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">{items.map((i) => <InspectionCard key={i.id} model={fromSummary(i, t, formatters)} />)}</div>
    </section>
  );
}
