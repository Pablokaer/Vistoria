"use client";

import Link from "next/link";
import { useGreeting } from "@/components/AppShell";
import { DashboardPipeline } from "@/components/company/DashboardPipeline";
import { RecentInspectionList, RecentReportList, REPORT_STATUSES } from "@/components/company/RecentInspectionList";
import { Card, EmptyState, ErrorBanner, LinkButton, PageHeader, PageSkeleton, SectionLink } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyDashboardMessages } from "@/i18n/messages/company-dashboard";
import { useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { CompanyDashboard } from "@/lib/types";

function PortfolioPanel({ data }: { data: CompanyDashboard }) {
  const t = useT(companyDashboardMessages);
  const rows: [string, number, string | null][] = [
    [t("properties"), data.properties, "/company/properties"],
    [t("completedReports"), data.completed, null],
  ];
  return (
    <Card title={t("portfolio")} actions={<SectionLink href="/company/properties">{t("managePortfolio")}</SectionLink>}>
      <dl className="grid grid-cols-2 gap-3">
        {rows.map(([label, value, href]) => {
          const body = <><dt className="text-caption font-medium text-ink-3">{label}</dt><dd className="tabular text-metric font-semibold text-ink">{value}</dd></>;
          return href
            ? <Link key={label} href={href} className="rounded-md bg-surface-2 px-3 py-2.5 transition hover:bg-neutral-50">{body}</Link>
            : <div key={label} className="rounded-md bg-surface-2 px-3 py-2.5">{body}</div>;
        })}
      </dl>
    </Card>
  );
}

function DashboardBody({ data }: { data: CompanyDashboard }) {
  const t = useT(companyDashboardMessages);
  const reports = data.recentInspections.filter((i) => REPORT_STATUSES.includes(i.status)).slice(0, 5);
  return (
    <div className="space-y-8">
      <DashboardPipeline data={data} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card flush title={t("recentInspections")}>
          {data.recentInspections.length === 0
            ? <div className="p-4 sm:p-5"><EmptyState icon="clipboard" title={t("noInspectionsYet")}
                action={<LinkButton href="/company/properties/new" icon="plus" size="sm">{t("addProperty")}</LinkButton>}>{t("noInspectionsHint")}</EmptyState></div>
            : <RecentInspectionList items={data.recentInspections} />}
        </Card>
        <div className="space-y-6">
          <PortfolioPanel data={data} />
          <Card flush title={t("recentReports")}><RecentReportList items={reports} /></Card>
        </div>
      </div>
    </div>
  );
}

/** Company home: where work is waiting in the pipeline, recent activity and the portfolio at a glance. */
export default function CompanyDashboardPage() {
  const { user } = useAuth();
  const t = useT(companyDashboardMessages);
  const greeting = useGreeting();
  const { data, error, loading, reload } = useApi<CompanyDashboard>("/api/company/dashboard");

  return (
    <>
      <PageHeader eyebrow={greeting} title={user?.company?.companyName ?? t("dashboard")} subtitle={t("dashboardSubtitle")}
        actions={<>
          <LinkButton href="/company/properties/new" variant="secondary" icon="building">{t("addProperty")}</LinkButton>
          <LinkButton href="/company/inspections/new" icon="plus">{t("newInspection")}</LinkButton>
        </>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <PageSkeleton /> : data && <DashboardBody data={data} />}
    </>
  );
}
