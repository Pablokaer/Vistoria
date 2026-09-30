"use client";

import Link from "next/link";
import { Badge, Card, EmptyState, ErrorBanner, LinkButton, Loading, PageHeader, Stat } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { CompanyDashboard } from "@/lib/types";

function RecentInspections({ items }: { items: CompanyDashboard["recentInspections"] }) {
  const t = useT(companyPageMessages);
  const { formatDateTime, humanize } = useFormatters();
  if (items.length === 0) return <EmptyState title={t("noInspectionsYet")}>{t("noInspectionsHint")}</EmptyState>;
  return (
    <ul className="divide-y divide-slate-100">
      {items.map((i) => (
        <li key={i.id}>
          <Link href={`/company/inspections/${i.id}`} className="flex flex-wrap items-center justify-between gap-2 py-3 hover:bg-slate-50">
            <span>
              <span className="block font-medium text-slate-900">{i.propertyAddress}</span>
              <span className="text-sm text-slate-600">{humanize(i.inspectionType)} · {i.agentName ?? t("noAgentYet")} · {formatDateTime(i.updatedAt)}</span>
            </span>
            <Badge value={i.status} />
          </Link>
        </li>
      ))}
    </ul>
  );
}

export default function CompanyDashboardPage() {
  const { user } = useAuth();
  const t = useT(companyPageMessages);
  const { data, error, loading, reload } = useApi<CompanyDashboard>("/api/company/dashboard");

  return (
    <>
      <PageHeader title={user?.company?.companyName ?? t("dashboard")} subtitle={t("dashboardSubtitle")}
        actions={<><LinkButton href="/company/properties/new" variant="secondary">{t("addProperty")}</LinkButton><LinkButton href="/company/inspections/new">{t("newInspection")}</LinkButton></>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            <Stat label={t("statProperties")} value={data.properties} href="/company/properties" />
            <Stat label={t("statOpen")} value={data.open + data.draft} />
            <Stat label={t("statAssigned")} value={data.assigned} />
            <Stat label={t("statInProgress")} value={data.inProgress + data.inReview} />
            <Stat label={t("statAwaitingTenant")} value={data.awaitingTenant} />
            <Stat label={t("statCompleted")} value={data.completed} />
          </div>
          {data.disputed > 0 && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{t("disputedCount", { count: data.disputed })}</div>
          )}
          <Card title={t("recentInspections")}><RecentInspections items={data.recentInspections} /></Card>
        </div>
      )}
    </>
  );
}
