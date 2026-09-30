"use client";

import { ActiveInspectionPanel } from "@/components/agent/ActiveInspectionPanel";
import { AgentMetrics } from "@/components/agent/AgentMetrics";
import { pickCurrent } from "@/components/agent/activeInspection";
import { AvailablePreview, OtherActive, RecentlyCompleted } from "@/components/agent/DashboardLists";
import { useGreeting } from "@/components/AppShell";
import { EmptyState, ErrorBanner, LinkButton, PageHeader, PageSkeleton } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentDashboardMessages } from "@/i18n/messages/agent-dashboard";
import { useApi } from "@/lib/hooks";
import type { AgentDashboard } from "@/lib/types";

/** No active work: one compact nudge towards the marketplace (or reassurance when it is empty). */
function NoActiveInspection({ available }: { available: number }) {
  const d = useT(agentDashboardMessages);
  return (
    <EmptyState icon="inspections" title={d("noActiveTitle")}
      action={available > 0 ? <LinkButton href="/agent/available" icon="search">{d("browseInspections")}</LinkButton> : undefined}>
      {available > 0 ? d("noActiveBody") : d("noActiveNoneAvailable")}
    </EmptyState>
  );
}

/**
 * The inspector's operational hub, ordered by what they need: workload counters, the inspection to continue now,
 * other active work, then new opportunities and recently finalized reports.
 */
export default function AgentDashboardPage() {
  const d = useT(agentDashboardMessages);
  const greeting = useGreeting();
  const { data, error, loading, reload } = useApi<AgentDashboard>("/api/agent/dashboard");
  const current = data ? pickCurrent(data.active) : null;
  return (
    <>
      <PageHeader eyebrow={greeting} title={d("title")} subtitle={d("subtitle")}
        actions={<LinkButton href="/agent/available" icon="search">{d("browseInspections")}</LinkButton>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <PageSkeleton /> : data && (
        <div className="space-y-8">
          <AgentMetrics data={data} />
          <div className="grid grid-cols-1 gap-8 lg:grid-cols-[minmax(0,1fr)_22rem]">
            <div className="min-w-0 space-y-8">
              {current ? <ActiveInspectionPanel item={current} /> : <NoActiveInspection available={data.available} />}
              <OtherActive items={data.active.filter((i) => i.id !== current?.id)} />
            </div>
            <div className="min-w-0 space-y-8">
              <AvailablePreview />
              <RecentlyCompleted />
            </div>
          </div>
        </div>
      )}
    </>
  );
}
