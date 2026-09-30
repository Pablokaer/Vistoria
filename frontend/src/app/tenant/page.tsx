"use client";

import Link from "next/link";
import { Badge, EmptyState, ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { formatDate, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { TenantDashboard, TenantInspection } from "@/lib/types";

function List({ title, items, empty }: { title: string; items: TenantInspection[]; empty: string }) {
  return (
    <section>
      <h2 className="mb-3 text-base font-semibold">{title} ({items.length})</h2>
      {items.length === 0 ? <EmptyState title={empty} /> : (
        <div className="space-y-3">
          {items.map((i) => (
            <Link key={i.id} href={`/tenant/inspections/${i.id}`} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-slate-200 bg-white p-4 shadow-sm hover:border-brand/40">
              <div>
                <div className="font-medium">{humanize(i.inspectionType)} · {i.propertyAddress}</div>
                <div className="text-sm text-slate-600">{i.companyName} · inspected {formatDate(i.completedAt)}</div>
              </div>
              <Badge value={i.status} />
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}

export default function TenantDashboardPage() {
  const { data, error, loading, reload } = useApi<TenantDashboard>("/api/tenant/dashboard");
  return (
    <>
      <PageHeader title="My inspections" subtitle="Reports shared with you by your letting company" />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-8">
          <List title="Awaiting your review" items={data.awaitingReview} empty="Nothing to review right now." />
          <List title="Accepted" items={data.accepted} empty="No accepted reports." />
          <List title="Disputed" items={data.disputed} empty="No disputed reports." />
        </div>
      )}
    </>
  );
}
