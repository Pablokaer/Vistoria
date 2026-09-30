"use client";

import { MetricCard, Notice } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyDashboardMessages } from "@/i18n/messages/company-dashboard";
import type { CompanyDashboard } from "@/lib/types";

/**
 * Where the company's inspections are in their life cycle, in the order they move through it. Each figure says
 * what is happening, so the owner sees at a glance where work is waiting (on an inspector, the inspector, a tenant).
 */
export function DashboardPipeline({ data }: { data: CompanyDashboard }) {
  const t = useT(companyDashboardMessages);
  return (
    <section aria-labelledby="pipeline-title" className="space-y-3">
      <h2 id="pipeline-title" className="text-section font-semibold text-ink">{t("needsAttention")}</h2>
      {/* Phones: a swipeable strip keeps the pipeline to one row; tablets and up: a grid. */}
      <div className="-mx-4 flex snap-x gap-3 overflow-x-auto px-4 pb-1 sm:-mx-6 sm:px-6 md:mx-0 md:grid md:grid-cols-3 md:overflow-visible md:px-0 xl:grid-cols-5 [&>*]:w-44 [&>*]:shrink-0 [&>*]:snap-start md:[&>*]:w-auto">
        <MetricCard icon="globe" tone="info" label={t("awaitingInspector")} value={data.open}
          hint={data.draft > 0 ? t("draftsHint", { count: data.draft }) : t("awaitingInspectorHint")} />
        <MetricCard icon="play" tone="warning" label={t("inProgress")} value={data.assigned + data.inProgress}
          hint={data.assigned > 0 ? t("inProgressHint", { count: data.assigned }) : t("inProgressIdle")} />
        <MetricCard icon="eye" tone="review" label={t("inReview")} value={data.inReview} hint={t("inReviewHint")} />
        <MetricCard icon="clock" tone="brand" label={t("awaitingTenant")} value={data.awaitingTenant} hint={t("awaitingTenantHint")} />
        <MetricCard icon="flag" tone={data.disputed > 0 ? "warning" : "neutral"} emphasis={data.disputed > 0}
          label={t("disputed")} value={data.disputed} hint={data.disputed > 0 ? t("disputedHint") : t("disputedNone")} />
      </div>
      {data.disputed > 0 && <Notice tone="warning">{t("disputedCount", { count: data.disputed })}</Notice>}
    </section>
  );
}
