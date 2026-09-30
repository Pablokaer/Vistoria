"use client";

import { MetricCard } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentDashboardMessages } from "@/i18n/messages/agent-dashboard";
import { agentMessages } from "@/i18n/messages/agent";
import type { AgentDashboard } from "@/lib/types";

/** The four counters of the inspector's workload; each links to the list it counts. */
export function AgentMetrics({ data }: { data: AgentDashboard }) {
  const t = useT(agentMessages);
  const d = useT(agentDashboardMessages);
  const inProgress = data.inProgress + data.inReview;
  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <MetricCard icon="globe" tone="info" label={t("statAvailable")} value={data.available} hint={d("metricAvailableHint")} href="/agent/available" />
      <MetricCard icon="userCheck" tone="brand" label={t("statAssigned")} value={data.assigned} hint={d("metricAssignedHint")}
        href="/agent/inspections?tab=assigned" emphasis={data.assigned > 0 && data.inProgress === 0} />
      <MetricCard icon="play" tone="warning" label={t("statInProgress")} value={inProgress}
        hint={data.inReview > 0 ? d("metricInProgressReviewHint", { count: data.inReview }) : d("metricInProgressHint")}
        href="/agent/inspections?tab=inProgress" emphasis={data.inProgress > 0} />
      <MetricCard icon="checkCircle" tone="success" label={t("statCompleted")} value={data.completed} hint={d("metricCompletedHint")} href="/agent/completed" />
    </div>
  );
}
