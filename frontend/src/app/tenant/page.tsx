"use client";

import Link from "next/link";
import { Badge, EmptyState, ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";
import { useApi } from "@/lib/hooks";
import type { TenantDashboard, TenantInspection } from "@/lib/types";

function List({ title, items, empty }: { title: string; items: TenantInspection[]; empty: string }) {
  const t = useT(tenantMessages);
  const { formatDate, humanize } = useFormatters();
  return (
    <section>
      <h2 className="mb-3 text-base font-semibold">{title} ({items.length})</h2>
      {items.length === 0 ? <EmptyState title={empty} /> : (
        <div className="space-y-3">
          {items.map((i) => (
            <Link key={i.id} href={`/tenant/inspections/${i.id}`} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-slate-200 bg-white p-4 shadow-sm hover:border-brand/40">
              <div>
                <div className="font-medium">{humanize(i.inspectionType)} · {i.propertyAddress}</div>
                <div className="text-sm text-slate-600">{t("companyInspected", { company: i.companyName, date: formatDate(i.completedAt) })}</div>
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
  const t = useT(tenantMessages);
  const { data, error, loading, reload } = useApi<TenantDashboard>("/api/tenant/dashboard");
  return (
    <>
      <PageHeader title={t("myInspections")} subtitle={t("dashboardSubtitle")} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-8">
          <List title={t("awaitingReview")} items={data.awaitingReview} empty={t("nothingToReview")} />
          <List title={t("accepted")} items={data.accepted} empty={t("noAccepted")} />
          <List title={t("disputed")} items={data.disputed} empty={t("noDisputed")} />
        </div>
      )}
    </>
  );
}
