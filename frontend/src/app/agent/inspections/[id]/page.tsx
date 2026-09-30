"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, DefinitionList, ErrorBanner, LinkButton, Loading, Notice, PageHeader, ProgressBar } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { errorMessage, post } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails } from "@/lib/types";

export default function AgentInspectionPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const t = useT(agentMessages);
  const { formatDate, formatDateTime, humanize } = useFormatters();
  const { data: i, error, loading, reload } = useApi<InspectionDetails>(`/api/agent/inspections/${id}`);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  if (loading && !i) return <Loading />;
  if (!i) return <ErrorBanner message={error ?? t("inspectionNotFound")} onRetry={reload} />;

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
        subtitle={address} back={{ href: "/agent/inspections", label: t("myInspectionsTitle") }}
        actions={<>
          {i.report && <LinkButton href={`/reports/${i.report.reportId}`}>{t("viewReport")}</LinkButton>}
          {i.status === "Review" && <LinkButton href={`/agent/inspections/${id}/review`}>{t("continueReview")}</LinkButton>}
        </>} />
      <ErrorBanner message={actionError} />

      {i.status === "Assigned" && (
        <Card className="mb-4">
          <p className="mb-4 text-sm text-slate-700">{t("startHint")}</p>
          <Button size="lg" className="w-full sm:w-auto" loading={busy} onClick={() => void start()}>{t("startInspection")}</Button>
        </Card>
      )}
      {i.completedAt && <Notice tone="success">{t("finalizedOn", { date: formatDateTime(i.completedAt) })}</Notice>}

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2" title={t("rooms")}>
          <ProgressBar value={i.roomsCompleted} total={i.rooms.length} />
          <ul className="mt-4 space-y-2">
            {i.rooms.map((r) => {
              const content = (
                <div className="flex min-h-14 items-center justify-between gap-3 rounded-lg border border-slate-200 px-4 py-3">
                  <div>
                    <div className="font-medium">{r.sequence}. {r.name}</div>
                    <div className="text-xs text-slate-500">
                      {t("roomPhotoCount", { count: r.generalPhotoCount })}{r.defectCount > 0 && t("roomDefectCount", { count: r.defectCount })}
                      {r.hasFinalDescription ? t("roomDescribed") : ""}{r.comparisonDecided === false ? t("roomComparisonPending") : ""}
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
                <LinkButton href={`/agent/inspections/${id}/capture`}>{t("photos")}</LinkButton>
                <LinkButton variant="secondary" href={`/agent/inspections/${id}/descriptions`}>{t("descriptions")}</LinkButton>
              </>}
              <Button variant={can("submitForReview") ? "primary" : "secondary"} disabled={!can("submitForReview")} loading={busy} onClick={() => void review()}>
                {t("reviewInspection")}
              </Button>
            </div>
          )}
          {i.status === "InProgress" && !can("submitForReview") && <p className="mt-2 text-xs text-slate-500">{t("reviewHint")}</p>}
        </Card>
        <div className="space-y-4">
          <Card title={t("details")}>
            <DefinitionList items={[
              [t("company"), i.companyName], [t("property"), humanize(i.property.propertyType)], [t("scheduled"), formatDate(i.scheduledDate)],
              [t("tenants"), i.tenancy?.members.map((m) => m.fullName).join(", ") || "—"],
              ...(i.comparisonInspection ? [[t("baseline"), t("baselineValue", { number: i.comparisonInspection.reportNumber ?? "", date: formatDate(i.comparisonInspection.completedAt) })] as [string, string]] : []),
            ]} />
          </Card>
          {i.instructions && <Card title={t("instructions")}><p className="whitespace-pre-line text-sm text-slate-700">{i.instructions}</p></Card>}
        </div>
      </div>
    </>
  );
}
