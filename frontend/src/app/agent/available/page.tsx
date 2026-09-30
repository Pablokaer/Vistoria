"use client";

import { AvailableCard } from "@/components/InspectionLists";
import { Button, EmptyState, ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

export default function AvailablePage() {
  const { data, error, loading, reload } = useApi<AvailableInspection[]>("/api/agent/available");
  return (
    <>
      <PageHeader title="Available inspections" subtitle="Public inspections waiting for an inspector. Exact addresses are shown after you accept."
        actions={<Button variant="secondary" onClick={() => void reload()} loading={loading}>Refresh</Button>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data?.length === 0 ? <EmptyState title="No inspections available right now">Check back later.</EmptyState> : (
        <div className="grid gap-3 sm:grid-cols-2">{data?.map((i) => <AvailableCard key={i.id} item={i} />)}</div>
      )}
    </>
  );
}
