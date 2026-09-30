"use client";

import { useGreeting } from "@/components/AppShell";
import { AwaitingReviewCard, TenantHistory } from "@/components/tenant/TenantReportCards";
import { EmptyState, ErrorBanner, PageHeader, SectionHeader, Skeleton } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";
import { useApi } from "@/lib/hooks";
import type { TenantDashboard, TenantInspection } from "@/lib/types";

const byResponse = (a: TenantInspection, b: TenantInspection) => (b.respondedAt ?? "").localeCompare(a.respondedAt ?? "");

function DashboardSkeleton() {
  return <div className="space-y-6" aria-busy="true"><Skeleton className="h-28" /><Skeleton className="h-5 w-32" /><Skeleton className="h-32" /></div>;
}

/**
 * The tenant's home is deliberately simple: what needs their review first, then what they already answered.
 */
export default function TenantDashboardPage() {
  const t = useT(tenantMessages);
  const greeting = useGreeting();
  const { data, error, loading, reload } = useApi<TenantDashboard>("/api/tenant/dashboard");
  const history = data ? [...data.accepted, ...data.disputed].sort(byResponse) : [];
  return (
    <div className="mx-auto max-w-narrow">
      <PageHeader eyebrow={greeting} title={t("dashboardTitle")} subtitle={t("dashboardSubtitle")} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <DashboardSkeleton /> : data && (
        <div className="space-y-8">
          <section>
            <SectionHeader title={t("awaitingReview")} count={data.awaitingReview.length} />
            {data.awaitingReview.length === 0
              ? <EmptyState icon="checkCircle" title={t("allCaughtUp")}>{t("allCaughtUpText")}</EmptyState>
              : (
                <div className="space-y-3">
                  <p className="text-label text-ink-3">{t("reviewHint")}</p>
                  {data.awaitingReview.map((i) => <AwaitingReviewCard key={i.id} item={i} />)}
                </div>
              )}
          </section>
          <section>
            <SectionHeader title={t("history")} count={history.length} />
            <TenantHistory items={history} />
          </section>
        </div>
      )}
    </div>
  );
}
