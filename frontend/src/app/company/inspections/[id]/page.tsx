"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { InspectionDetailsPanel, InspectionTenancyPanel, InvitationStatusPanel, NewInvitationPanel } from "@/components/company/inspection/InspectionInfoPanels";
import { InspectionTimeline } from "@/components/company/inspection/InspectionTimeline";
import { ReportPanel, RoomsProgressPanel } from "@/components/company/inspection/InspectionWorkPanels";
import { Button, ErrorBanner, InspectionTypeTag, LinkButton, Notice, PageHeader, PageSkeleton, StatusBadge, useConfirm, useToast, VisibilityTag } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails, PrivateInvitation, PublishResult } from "@/lib/types";

type Act = (name: string, fn: () => Promise<void>) => Promise<void>;

/** Header actions allowed by the API for the current status (allowedActions), primary action last. */
function InspectionActions({ i, busy, act, setData, setInvitation }:
  { i: InspectionDetails; busy: string | null; act: Act; setData: (d: InspectionDetails) => void; setInvitation: (p: PrivateInvitation | null) => void }) {
  const t = useT(companyInspectionMessages);
  const toast = useToast();
  const [confirm, dialog] = useConfirm();
  const can = (a: string) => i.allowedActions.includes(a);
  const publish = () => act("publish", async () => {
    const r = await post<PublishResult>(`/api/inspections/${i.id}/publish`);
    setData(r.inspection);
    setInvitation(r.invitation);
    toast.notify({ tone: "success", title: t("inspectionPublishedToast") });
  });
  const send = () => act("send", async () => { setData(await post(`/api/inspections/${i.id}/send-to-tenant`)); toast.notify({ tone: "success", title: t("sentToTenantToast") }); });
  const cancel = async () => {
    if (await confirm({ title: t("confirmCancel"), description: t("confirmCancelHint"), confirmLabel: t("cancel"), danger: true }))
      await act("cancel", async () => setData(await post(`/api/inspections/${i.id}/cancel`)));
  };
  return (
    <>
      {dialog}
      {can("cancel") && <Button variant="danger" loading={busy === "cancel"} onClick={() => void cancel()}>{t("cancel")}</Button>}
      {i.report && !can("sendToTenant") && <LinkButton href={`/reports/${i.report.reportId}`} variant="secondary" icon="file">{t("viewReport")}</LinkButton>}
      {can("sendToTenant") && <Button icon="share" loading={busy === "send"} onClick={() => void send()}>{t("sendToTenant")}</Button>}
      {can("publish") && <Button icon="globe" loading={busy === "publish"} onClick={() => void publish()}>{t("publish")}</Button>}
    </>
  );
}

export default function CompanyInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const { data: i, setData, error, loading, reload } = useApi<InspectionDetails>(`/api/inspections/${id}`);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [invitation, setInvitation] = useState<PrivateInvitation | null>(null);
  const t = useT(companyInspectionMessages);

  const act: Act = async (name, fn) => {
    setBusy(name);
    setActionError(null);
    try { await fn(); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  };

  if (loading && !i) return <PageSkeleton />;
  if (!i) return <ErrorBanner message={error ?? t("notFound")} onRetry={reload} />;
  const regenerate = i.allowedActions.includes("regenerateInvitation")
    ? () => void act("regen", async () => { setInvitation(await post(`/api/inspections/${id}/invitation`)); await reload(); }) : undefined;

  return (
    <>
      <PageHeader title={i.property.addressLine1}
        eyebrow={<span className="inline-flex items-center gap-2"><InspectionTypeTag type={i.inspectionType} /><StatusBadge value={i.status} /><VisibilityTag visibility={i.visibility} /></span>}
        subtitle={<Link className="hover:text-brand hover:underline" href={`/company/properties/${i.property.id}`}>{i.property.city} {i.property.postcode} · {t("openProperty")}</Link>}
        back={{ href: "/company", label: t("dashboard") }}
        actions={<InspectionActions i={i} busy={busy} act={act} setData={setData} setInvitation={setInvitation} />} />
      <ErrorBanner message={actionError} />
      {i.status === "Disputed" && <Notice tone="warning">{t("disputed")}</Notice>}
      {i.status === "Cancelled" && <Notice>{t("cancelledNotice")}</Notice>}
      {invitation && <NewInvitationPanel invitation={invitation} />}
      <div className="mb-6"><InspectionTimeline inspection={i} /></div>
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-6">
          {i.report && <ReportPanel report={i.report} />}
          <RoomsProgressPanel inspection={i} />
        </div>
        <div className="space-y-6">
          <InspectionDetailsPanel inspection={i} />
          {i.tenancy && <InspectionTenancyPanel tenancy={i.tenancy} />}
          {i.invitation && <InvitationStatusPanel invitation={i.invitation} onRegenerate={regenerate} busy={busy === "regen"} />}
        </div>
      </div>
    </>
  );
}
