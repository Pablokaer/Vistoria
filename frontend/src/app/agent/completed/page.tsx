"use client";

import { MyInspectionList } from "@/components/InspectionLists";
import { ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary } from "@/lib/types";

export default function CompletedPage() {
  const { data, error, loading, reload } = useApi<InspectionSummary[]>("/api/agent/inspections?scope=completed");
  return (
    <>
      <PageHeader title="Completed inspections" subtitle="Finalized reports (read-only)" />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : <MyInspectionList items={data ?? []} empty="No completed inspections yet." />}
    </>
  );
}
