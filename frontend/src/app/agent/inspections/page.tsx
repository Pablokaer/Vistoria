"use client";

import { MyInspectionList } from "@/components/InspectionLists";
import { ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary } from "@/lib/types";

export default function MyInspectionsPage() {
  const { data, error, loading, reload } = useApi<InspectionSummary[]>("/api/agent/inspections?scope=active");
  return (
    <>
      <PageHeader title="My inspections" subtitle="Assigned to you and in progress" />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : <MyInspectionList items={data ?? []} empty="Nothing assigned yet." />}
    </>
  );
}
