"use client";

import { AvailableCard } from "@/components/InspectionLists";
import { Button, EmptyState, ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

export default function AvailablePage() {
  const t = useT(agentMessages);
  const { data, error, loading, reload } = useApi<AvailableInspection[]>("/api/agent/available");
  return (
    <>
      <PageHeader title={t("availableTitle")} subtitle={t("availableSubtitle")}
        actions={<Button variant="secondary" onClick={() => void reload()} loading={loading}>{t("refresh")}</Button>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data?.length === 0 ? <EmptyState title={t("noneAvailable")}>{t("checkBackLater")}</EmptyState> : (
        <div className="grid gap-3 sm:grid-cols-2">{data?.map((i) => <AvailableCard key={i.id} item={i} />)}</div>
      )}
    </>
  );
}
