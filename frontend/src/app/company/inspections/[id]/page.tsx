"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, CopyField, DefinitionList, ErrorBanner, LinkButton, Loading, Notice, PageHeader, ProgressBar } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails, PrivateInvitation, PublishResult } from "@/lib/types";

export default function CompanyInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const { data: i, setData, error, loading, reload } = useApi<InspectionDetails>(`/api/inspections/${id}`);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [invitation, setInvitation] = useState<PrivateInvitation | null>(null);
  const [shareLink, setShareLink] = useState<string | null>(null);
  const t = useT(companyInspectionMessages);
  const { formatDate, formatDateTime, humanize } = useFormatters();

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setActionError(null);
    try { await fn(); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  }

  if (loading && !i) return <Loading />;
  if (!i) return <ErrorBanner message={error ?? t("notFound")} onRetry={reload} />;
  const can = (a: string) => i.allowedActions.includes(a);

  return (
    <>
      <PageHeader
        title={<span className="flex flex-wrap items-center gap-3">{t("titleSuffix", { type: humanize(i.inspectionType) })} <Badge value={i.status} /></span>}
        subtitle={<Link className="hover:underline" href={`/company/properties/${i.property.id}`}>{i.property.addressLine1}, {i.property.city} {i.property.postcode}</Link>}
        back={{ href: "/company", label: t("dashboard") }}
        actions={<>
          {can("publish") && <Button loading={busy === "publish"} onClick={() => act("publish", async () => { const r = await post<PublishResult>(`/api/inspections/${id}/publish`); setData(r.inspection); setInvitation(r.invitation); })}>{t("publish")}</Button>}
          {can("sendToTenant") && <Button loading={busy === "send"} onClick={() => act("send", async () => setData(await post(`/api/inspections/${id}/send-to-tenant`)))}>{t("sendToTenant")}</Button>}
          {i.report && <LinkButton href={`/reports/${i.report.reportId}`}>{t("viewReport")}</LinkButton>}
          {can("cancel") && <Button variant="danger" loading={busy === "cancel"} onClick={() => { if (confirm(t("confirmCancel"))) void act("cancel", async () => setData(await post(`/api/inspections/${id}/cancel`))); }}>{t("cancel")}</Button>}
        </>}
      />
      <ErrorBanner message={actionError} />
      {i.status === "Disputed" && <Notice tone="warning">{t("disputed")}</Notice>}
      {invitation && (
        <Card className="mb-4" title={t("privateInvitation")}>
          <Notice tone="warning">{t("shownOnce")}</Notice>
          <div className="space-y-3">
            <CopyField label={t("invitationLink")} value={invitation.link} />
            <div className="rounded-lg bg-slate-900 px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{invitation.accessCode}</div>
          </div>
        </Card>
      )}
      <div className="grid gap-4 lg:grid-cols-3">
        <div className="space-y-4 lg:col-span-2">
          <Card title={t("progress")}>
            <ProgressBar value={i.roomsCompleted} total={i.rooms.length} />
            <ul className="mt-4 grid gap-2 sm:grid-cols-2">
              {i.rooms.map((r) => (
                <li key={r.id} className="flex items-center justify-between rounded-lg border border-slate-200 px-3 py-2 text-sm">
                  <span><span className="font-medium">{r.name}</span><span className="text-slate-500">{t("roomPhotos", { count: r.generalPhotoCount })}{r.defectCount > 0 && t("roomDefects", { count: r.defectCount })}</span></span>
                  <Badge value={r.status} />
                </li>
              ))}
              {i.rooms.length === 0 && <li className="text-sm text-slate-500">{t("roomsCapturedOnPublish")}</li>}
            </ul>
          </Card>
          {i.report && (
            <Card title={t("report")} actions={<Button variant="secondary" loading={busy === "share"} onClick={() => act("share", async () => {
              const link = await post<{ link: string }>(`/api/reports/${i.report!.reportId}/share-links`, { days: 30 });
              setShareLink(link.link);
            })}>{t("createShareLink")}</Button>}>
              <DefinitionList items={[[t("reportNumber"), i.report.reportNumber], [t("version"), `v${i.report.latestVersion}`], [t("generated"), formatDateTime(i.report.generatedAt)]]} />
              {shareLink && <div className="mt-4"><CopyField label={t("readOnlyLink")} value={shareLink} /></div>}
            </Card>
          )}
        </div>
        <div className="space-y-4">
          <Card title={t("details")}>
            <DefinitionList items={[
              [t("visibility"), humanize(i.visibility)],
              [t("agent"), i.agent ? `${i.agent.fullName}${i.agent.phone ? ` · ${i.agent.phone}` : ""}` : t("notAssigned")],
              [t("scheduled"), formatDate(i.scheduledDate)],
              ...(i.acceptBy ? [[t("acceptBy"), formatDateTime(i.acceptBy)] as [string, string]] : []),
              ...(i.comparisonInspection ? [[t("comparedWith"), t("comparedWithValue", { reportNumber: i.comparisonInspection.reportNumber ?? "" })] as [string, string]] : []),
            ]} />
            {i.instructions && <p className="mt-3 whitespace-pre-line rounded-lg bg-slate-50 p-3 text-sm text-slate-700">{i.instructions}</p>}
          </Card>
          {i.tenancy && (
            <Card title={t("tenancy")}>
              <p className="text-sm text-slate-700">{formatDate(i.tenancy.startDate)} – {i.tenancy.endDate ? formatDate(i.tenancy.endDate) : t("ongoing")}</p>
              <ul className="mt-2 space-y-1 text-sm">
                {i.tenancy.members.map((m) => <li key={m.id}>{m.fullName} <span className="text-slate-500">({m.joined ? t("joinedLower") : t("invitedLower")})</span></li>)}
                {i.tenancy.members.length === 0 && <li className="text-slate-500">{t("noTenantsWaiting")}</li>}
              </ul>
            </Card>
          )}
          {i.invitation && (
            <Card title={t("privateInvitation")} actions={can("regenerateInvitation") && (
              <Button variant="ghost" loading={busy === "regen"} onClick={() => act("regen", async () => { setInvitation(await post(`/api/inspections/${id}/invitation`)); await reload(); })}>{t("newLinkAndCode")}</Button>)}>
              <DefinitionList items={[
                [t("status"), i.invitation.used ? t("usedByAgent") : i.invitation.revoked ? t("revoked") : i.invitation.expired ? t("expired") : t("active")],
                [t("expires"), formatDateTime(i.invitation.expiresAt)],
                [t("failedAttempts"), `${i.invitation.attemptCount} / ${i.invitation.maxAttempts}`],
              ]} />
            </Card>
          )}
          <Card title={t("timeline")}>
            <DefinitionList items={([
              [t("created"), i.createdAt], [t("published"), i.publishedAt], [t("accepted"), i.acceptedAt], [t("started"), i.startedAt],
              [t("finalized"), i.completedAt], [t("sentToTenant"), i.sentToTenantAt], [t("tenantResponded"), i.tenantRespondedAt],
            ] as [string, string | null][]).filter(([, v]) => v).map(([k, v]) => [k, formatDateTime(v)])} />
          </Card>
        </div>
      </div>
    </>
  );
}
