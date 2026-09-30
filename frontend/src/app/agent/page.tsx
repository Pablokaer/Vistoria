"use client";

import { MyInspectionList } from "@/components/InspectionLists";
import { ErrorBanner, LinkButton, Loading, PageHeader, Stat } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { AgentDashboard } from "@/lib/types";

export default function AgentDashboardPage() {
  const t = useT(agentMessages);
  const { data, error, loading, reload } = useApi<AgentDashboard>("/api/agent/dashboard");
  return (
    <>
      <PageHeader title={t("dashboardTitle")} actions={<LinkButton href="/agent/available">{t("findInspections")}</LinkButton>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Stat label={t("statAvailable")} value={data.available} href="/agent/available" />
            <Stat label={t("statAssigned")} value={data.assigned} href="/agent/inspections" />
            <Stat label={t("statInProgress")} value={data.inProgress + data.inReview} href="/agent/inspections" />
            <Stat label={t("statCompleted")} value={data.completed} href="/agent/completed" />
          </div>
          <div>
            <h2 className="mb-3 text-base font-semibold">{t("activeInspections")}</h2>
            <MyInspectionList items={data.active} empty={t("noActive")} />
          </div>
        </div>
      )}
    </>
  );
}
