"use client";

import { MyInspectionList } from "@/components/InspectionLists";
import { ErrorBanner, LinkButton, Loading, PageHeader, Stat } from "@/components/ui";
import { useApi } from "@/lib/hooks";
import type { AgentDashboard } from "@/lib/types";

export default function AgentDashboardPage() {
  const { data, error, loading, reload } = useApi<AgentDashboard>("/api/agent/dashboard");
  return (
    <>
      <PageHeader title="Inspector dashboard" actions={<LinkButton href="/agent/available">Find inspections</LinkButton>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Stat label="Available" value={data.available} href="/agent/available" />
            <Stat label="Assigned" value={data.assigned} href="/agent/inspections" />
            <Stat label="In progress" value={data.inProgress + data.inReview} href="/agent/inspections" />
            <Stat label="Completed" value={data.completed} href="/agent/completed" />
          </div>
          <div>
            <h2 className="mb-3 text-base font-semibold">Your active inspections</h2>
            <MyInspectionList items={data.active} empty="No active inspections. Accept one from the marketplace." />
          </div>
        </div>
      )}
    </>
  );
}
