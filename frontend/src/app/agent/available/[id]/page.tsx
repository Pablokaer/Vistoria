"use client";

import { useParams } from "next/navigation";
import { AcceptPanel } from "@/components/AcceptPanel";
import { ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

export default function AvailableDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data, error, loading } = useApi<AvailableInspection>(`/api/agent/available/${id}`);
  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader title={data ? `${humanize(data.inspectionType)} in ${data.city}` : "Inspection"} back={{ href: "/agent/available", label: "Available inspections" }} />
      {loading ? <Loading /> : error ? <ErrorBanner message={error === "Inspection was not found." || error.includes("not found") ? "This inspection is no longer available." : error} /> : data && <AcceptPanel item={data} />}
    </div>
  );
}
