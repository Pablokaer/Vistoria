"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, DefinitionList, ErrorBanner, LinkButton, Loading, Notice, PageHeader, ProgressBar } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { formatDate, formatDateTime, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails } from "@/lib/types";

export default function AgentInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { data: i, error, loading, reload } = useApi<InspectionDetails>(`/api/agent/inspections/${id}`);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  if (loading && !i) return <Loading />;
  if (!i) return <ErrorBanner message={error ?? "Inspection not found"} onRetry={reload} />;

  const can = (a: string) => i.allowedActions.includes(a);
  const address = [i.property.addressLine1, i.property.addressLine2, i.property.city, i.property.postcode].filter(Boolean).join(", ");

  async function start() {
    setBusy(true);
    setActionError(null);
    try { await post<InspectionDetails>(`/api/agent/inspections/${id}/start`); router.push(`/agent/inspections/${id}/capture`); }
    catch (e) { setActionError(errorMessage(e)); }
    finally { setBusy(false); }
  }

  async function review() {
    setBusy(true);
    setActionError(null);
    try {
      if (i!.status === "InProgress") await post(`/api/agent/inspections/${id}/submit-review`);
      router.push(`/agent/inspections/${id}/review`);
    } catch (e) { setActionError(errorMessage(e)); setBusy(false); }
  }

  return (
    <>
      <PageHeader title={<span className="flex flex-wrap items-center gap-3">{humanize(i.inspectionType)} <Badge value={i.status} /></span>}
        subtitle={address} back={{ href: "/agent/inspections", label: "My inspections" }}
        actions={<>
          {i.report && <LinkButton href={`/reports/${i.report.reportId}`}>View report</LinkButton>}
          {i.status === "Review" && <LinkButton href={`/agent/inspections/${id}/review`}>Continue review</LinkButton>}
        </>} />
      <ErrorBanner message={actionError} />

      {i.status === "Assigned" && (
        <Card className="mb-4">
          <p className="mb-4 text-sm text-slate-700">When you arrive at the property, start the inspection. You will first photograph every room, then check the descriptions.</p>
          <Button size="lg" className="w-full sm:w-auto" loading={busy} onClick={() => void start()}>Start inspection</Button>
        </Card>
      )}
      {i.completedAt && <Notice tone="success">Finalized on {formatDateTime(i.completedAt)}. The report is locked and can no longer be changed.</Notice>}

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2" title="Rooms">
          <ProgressBar value={i.roomsCompleted} total={i.rooms.length} />
          <ul className="mt-4 space-y-2">
            {i.rooms.map((r) => {
              const content = (
                <div className="flex min-h-14 items-center justify-between gap-3 rounded-lg border border-slate-200 px-4 py-3">
                  <div>
                    <div className="font-medium">{r.sequence}. {r.name}</div>
                    <div className="text-xs text-slate-500">
                      {r.generalPhotoCount} photo(s){r.defectCount > 0 && ` · ${r.defectCount} defect(s)`}
                      {r.hasFinalDescription ? " · described" : ""}{r.comparisonDecided === false ? " · comparison pending" : ""}
                    </div>
                  </div>
                  <Badge value={r.status} />
                </div>
              );
              return (
                <li key={r.id}>
                  {can("edit") ? <Link href={`/agent/inspections/${id}/capture#room-${r.id}`} className="block hover:bg-slate-50">{content}</Link> : content}
                </li>
              );
            })}
          </ul>
          {can("edit") && i.status === "InProgress" && (
            <div className="mt-4 flex flex-wrap gap-2">
              {i.roomsCompleted < i.rooms.length && <>
                <LinkButton href={`/agent/inspections/${id}/capture`}>Photos</LinkButton>
                <LinkButton variant="secondary" href={`/agent/inspections/${id}/descriptions`}>Descriptions</LinkButton>
              </>}
              <Button variant={can("submitForReview") ? "primary" : "secondary"} disabled={!can("submitForReview")} loading={busy} onClick={() => void review()}>
                Review inspection
              </Button>
            </div>
          )}
          {i.status === "InProgress" && !can("submitForReview") && <p className="mt-2 text-xs text-slate-500">Complete every room on the descriptions page to enable the review.</p>}
        </Card>
        <div className="space-y-4">
          <Card title="Details">
            <DefinitionList items={[
              ["Company", i.companyName], ["Property", humanize(i.property.propertyType)], ["Scheduled", formatDate(i.scheduledDate)],
              ["Tenants", i.tenancy?.members.map((m) => m.fullName).join(", ") || "—"],
              ...(i.comparisonInspection ? [["Baseline", `Move In ${i.comparisonInspection.reportNumber ?? ""} (${formatDate(i.comparisonInspection.completedAt)})`] as [string, string]] : []),
            ]} />
          </Card>
          {i.instructions && <Card title="Instructions"><p className="whitespace-pre-line text-sm text-slate-700">{i.instructions}</p></Card>}
        </div>
      </div>
    </>
  );
}
