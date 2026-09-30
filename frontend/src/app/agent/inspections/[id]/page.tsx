"use client";

import { useParams } from "next/navigation";
import { InspectionFacts, RoomProgressList } from "@/components/agent/InspectionOverview";
import { NextStepPanel } from "@/components/agent/NextStepPanel";
import { ErrorBanner, InspectionTypeTag, PageHeader, PageSkeleton, StatusBadge, VisibilityTag } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails } from "@/lib/types";

/**
 * Inspection overview for the assigned inspector: who/where in the header, the next action right below,
 * then rooms (the work) beside the reference facts.
 */
export default function AgentInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const t = useT(agentMessages);
  const { data: i, error, loading, reload } = useApi<InspectionDetails>(`/api/agent/inspections/${id}`);

  if (loading && !i) return <PageSkeleton />;
  if (!i) return <ErrorBanner message={error ?? t("inspectionNotFound")} onRetry={reload} />;

  const cityLine = [i.property.addressLine2, i.property.city, i.property.postcode].filter(Boolean).join(", ");
  return (
    <>
      <PageHeader back={{ href: "/agent/inspections", label: t("myInspectionsTitle") }} eyebrow={i.companyName}
        title={i.property.addressLine1} subtitle={cityLine}
        meta={<><InspectionTypeTag type={i.inspectionType} /><StatusBadge value={i.status} /><VisibilityTag visibility={i.visibility} /></>} />
      <div className="space-y-6">
        <NextStepPanel inspection={i} />
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
          <RoomProgressList inspection={i} editable={i.allowedActions.includes("edit")} />
          <InspectionFacts inspection={i} />
        </div>
      </div>
    </>
  );
}
