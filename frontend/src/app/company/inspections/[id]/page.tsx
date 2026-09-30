"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, CopyField, DefinitionList, ErrorBanner, LinkButton, Loading, Notice, PageHeader, ProgressBar } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { formatDate, formatDateTime, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails, PrivateInvitation, PublishResult } from "@/lib/types";

export default function CompanyInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const { data: i, setData, error, loading, reload } = useApi<InspectionDetails>(`/api/inspections/${id}`);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [invitation, setInvitation] = useState<PrivateInvitation | null>(null);
  const [shareLink, setShareLink] = useState<string | null>(null);

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setActionError(null);
    try { await fn(); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  }

  if (loading && !i) return <Loading />;
  if (!i) return <ErrorBanner message={error ?? "Inspection not found"} onRetry={reload} />;
  const can = (a: string) => i.allowedActions.includes(a);

  return (
    <>
      <PageHeader
        title={<span className="flex flex-wrap items-center gap-3">{humanize(i.inspectionType)} inspection <Badge value={i.status} /></span>}
        subtitle={<Link className="hover:underline" href={`/company/properties/${i.property.id}`}>{i.property.addressLine1}, {i.property.city} {i.property.postcode}</Link>}
        back={{ href: "/company", label: "Dashboard" }}
        actions={<>
          {can("publish") && <Button loading={busy === "publish"} onClick={() => act("publish", async () => { const r = await post<PublishResult>(`/api/inspections/${id}/publish`); setData(r.inspection); setInvitation(r.invitation); })}>Publish</Button>}
          {can("sendToTenant") && <Button loading={busy === "send"} onClick={() => act("send", async () => setData(await post(`/api/inspections/${id}/send-to-tenant`)))}>Send to tenant</Button>}
          {i.report && <LinkButton href={`/reports/${i.report.reportId}`}>View report</LinkButton>}
          {can("cancel") && <Button variant="danger" loading={busy === "cancel"} onClick={() => { if (confirm("Cancel this inspection?")) void act("cancel", async () => setData(await post(`/api/inspections/${id}/cancel`))); }}>Cancel</Button>}
        </>}
      />
      <ErrorBanner message={actionError} />
      {i.status === "Disputed" && <Notice tone="warning">The tenant disputed this report. Review their comments on the report page.</Notice>}
      {invitation && (
        <Card className="mb-4" title="Private invitation">
          <Notice tone="warning">Shown only once. Share the link and the code through different channels.</Notice>
          <div className="space-y-3">
            <CopyField label="Invitation link" value={invitation.link} />
            <div className="rounded-lg bg-slate-900 px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{invitation.accessCode}</div>
          </div>
        </Card>
      )}
      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <Card title="Progress">
            <ProgressBar value={i.roomsCompleted} total={i.rooms.length} />
            <ul className="mt-4 grid gap-2 sm:grid-cols-2">
              {i.rooms.map((r) => (
                <li key={r.id} className="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-sm">
                  <span><span className="font-medium">{r.name}</span><span className="text-slate-500"> · {r.generalPhotoCount} photos{r.defectCount > 0 && ` · ${r.defectCount} defects`}</span></span>
                  <Badge value={r.status} />
                </li>
              ))}
              {i.rooms.length === 0 && <li className="text-sm text-slate-500">Rooms are captured from the property when the inspection is published.</li>}
            </ul>
          </Card>
          {i.report && (
            <Card title="Report" actions={<Button variant="secondary" loading={busy === "share"} onClick={() => act("share", async () => {
              const link = await post<{ link: string }>(`/api/reports/${i.report!.reportId}/share-links`, { days: 30 });
              setShareLink(link.link);
            })}>Create share link</Button>}>
              <DefinitionList items={[["Report number", i.report.reportNumber], ["Version", `v${i.report.latestVersion}`], ["Generated", formatDateTime(i.report.generatedAt)]]} />
              {shareLink && <div className="mt-4"><CopyField label="Read-only link (30 days)" value={shareLink} /></div>}
            </Card>
          )}
        </div>
        <div className="space-y-4">
          <Card title="Details">
            <DefinitionList items={[
              ["Visibility", i.visibility],
              ["Agent", i.agent ? `${i.agent.fullName}${i.agent.phone ? ` · ${i.agent.phone}` : ""}` : "Not assigned"],
              ["Scheduled", formatDate(i.scheduledDate)],
              ...(i.acceptBy ? [["Accept by", formatDateTime(i.acceptBy)] as [string, string]] : []),
              ...(i.comparisonInspection ? [["Compared with", `Move In ${i.comparisonInspection.reportNumber ?? ""}`] as [string, string]] : []),
            ]} />
            {i.instructions && <p className="mt-3 whitespace-pre-line rounded-lg bg-slate-50 p-3 text-sm text-slate-700">{i.instructions}</p>}
          </Card>
          {i.tenancy && (
            <Card title="Tenancy">
              <p className="text-sm text-slate-700">{formatDate(i.tenancy.startDate)} – {i.tenancy.endDate ? formatDate(i.tenancy.endDate) : "ongoing"}</p>
              <ul className="mt-2 space-y-1 text-sm">
                {i.tenancy.members.map((m) => <li key={m.id}>{m.fullName} <span className="text-slate-500">({m.joined ? "joined" : "invited"})</span></li>)}
                {i.tenancy.members.length === 0 && <li className="text-slate-500">No tenants — the report will wait until you add one and send it.</li>}
              </ul>
            </Card>
          )}
          {i.invitation && (
            <Card title="Private invitation" actions={can("regenerateInvitation") && (
              <Button variant="ghost" loading={busy === "regen"} onClick={() => act("regen", async () => { setInvitation(await post(`/api/inspections/${id}/invitation`)); await reload(); })}>New link & code</Button>)}>
              <DefinitionList items={[
                ["Status", i.invitation.used ? "Used by an agent" : i.invitation.revoked ? "Revoked" : i.invitation.expired ? "Expired" : "Active"],
                ["Expires", formatDateTime(i.invitation.expiresAt)],
                ["Failed attempts", `${i.invitation.attemptCount} / ${i.invitation.maxAttempts}`],
              ]} />
            </Card>
          )}
          <Card title="Timeline">
            <DefinitionList items={([
              ["Created", i.createdAt], ["Published", i.publishedAt], ["Accepted", i.acceptedAt], ["Started", i.startedAt],
              ["Finalized", i.completedAt], ["Sent to tenant", i.sentToTenantAt], ["Tenant responded", i.tenantRespondedAt],
            ] as [string, string | null][]).filter(([, v]) => v).map(([k, v]) => [k, formatDateTime(v)])} />
          </Card>
        </div>
      </div>
    </>
  );
}
