"use client";

import { MyInspectionList } from "@/components/InspectionLists";
import { ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary } from "@/lib/types";

export default function CompletedPage() {
  const t = useT(agentMessages);
  const { data, error, loading, reload } = useApi<InspectionSummary[]>("/api/agent/inspections?scope=completed");
  return (
    <>
      <PageHeader title={t("completedTitle")} subtitle={t("completedSubtitle")} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : <MyInspectionList items={data ?? []} empty={t("noCompleted")} />}
    </>
  );
}
