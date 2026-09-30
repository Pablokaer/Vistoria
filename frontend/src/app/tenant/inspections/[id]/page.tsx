"use client";

import { useParams } from "next/navigation";
import { useState } from "react";
import { ReportBody, ReportMeta } from "@/components/ReportView";
import { Badge, Button, Card, ErrorBanner, Field, Loading, Notice, PageHeader, Textarea } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";
import { errorMessage, post } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { ReportView } from "@/lib/types";

export default function TenantReviewPage() {
  const { id } = useParams<{ id: string }>();
  const t = useT(tenantMessages);
  const { data, setData, error, loading, reload } = useApi<ReportView>(`/api/tenant/inspections/${id}/report`);
  const [mode, setMode] = useState<"choose" | "observe" | "dispute">("choose");
  const [general, setGeneral] = useState("");
  const [roomNote, setRoomNote] = useState<{ roomId: string; text: string } | null>(null);
  const [disputeReason, setDisputeReason] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function act(name: string, fn: () => Promise<ReportView>) {
    setBusy(name);
    setActionError(null);
    try { setData(await fn()); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  }

  if (loading && !data) return <Loading />;
  if (!data) return <ErrorBanner message={error ?? t("reportNotFound")} onRetry={reload} />;
  const open = data.canRespond;

  return (
    <div className={open ? "pb-32" : ""}>
      <PageHeader title={<span className="flex flex-wrap items-center gap-3">{t("inspectionReport")} <Badge value={data.inspectionStatus} /></span>}
        subtitle={`${data.snapshot.property.addressLine1}, ${data.snapshot.property.city} · ${data.reportNumber}`}
        back={{ href: "/tenant", label: t("myInspections") }}
        actions={data.pdfUrl && <a href={data.pdfUrl} target="_blank" rel="noreferrer" className="inline-flex min-h-10 items-center rounded-lg border border-slate-300 bg-white px-4 text-sm font-medium">{t("downloadPdf")}</a>} />
      <ErrorBanner message={actionError} />
      {open ? (
        <Notice>{t("readPrompt")}</Notice>
      ) : (
        <Notice tone={data.inspectionStatus === "Disputed" ? "warning" : "success"}>
          {data.inspectionStatus === "Accepted" ? t("youAccepted") : t("youDisputed")}
        </Notice>
      )}
      <div className="grid gap-4 lg:grid-cols-[1fr_300px]">
        <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} observations={data.observations} renderRoomExtra={(roomId) => open && (
          roomNote?.roomId === roomId ? (
            <div className="mt-3 space-y-2">
              <Textarea rows={3} value={roomNote.text} onChange={(e) => setRoomNote({ roomId, text: e.target.value })} placeholder={t("roomObservationPlaceholder")} />
              <div className="flex gap-2">
                <Button loading={busy === "room"} disabled={!roomNote.text.trim()} onClick={() => act("room", async () => {
                  const r = await post<ReportView>(`/api/tenant/inspections/${id}/observations`, { roomId, text: roomNote.text });
                  setRoomNote(null);
                  return r;
                })}>{t("addObservation")}</Button>
                <Button variant="secondary" onClick={() => setRoomNote(null)}>{t("cancel")}</Button>
              </div>
            </div>
          ) : <Button variant="ghost" className="mt-2" onClick={() => setRoomNote({ roomId, text: "" })}>{t("addRoomObservation")}</Button>
        )} />
        <div><ReportMeta report={data} /></div>
      </div>

      {open && (
        <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur">
          <div className="mx-auto max-w-5xl px-4 py-3">
            {mode === "choose" && (
              <div className="grid gap-2 sm:grid-cols-3">
                <Button size="lg" loading={busy === "accept"} onClick={() => { if (confirm(t("confirmAccept"))) void act("accept", () => post(`/api/tenant/inspections/${id}/accept`, { comment: t("everythingCorrect") })); }}>{t("everythingCorrect")}</Button>
                <Button size="lg" variant="secondary" onClick={() => setMode("observe")}>{t("wantObservations")}</Button>
                <Button size="lg" variant="danger" onClick={() => setMode("dispute")}>{t("disagree")}</Button>
              </div>
            )}
            {mode === "observe" && (
              <Card>
                <Field label={t("generalObservation")}><Textarea rows={3} value={general} onChange={(e) => setGeneral(e.target.value)} /></Field>
                <div className="mt-2 flex flex-wrap gap-2">
                  <Button loading={busy === "general"} disabled={!general.trim()} onClick={() => act("general", async () => {
                    const r = await post<ReportView>(`/api/tenant/inspections/${id}/observations`, { text: general });
                    setGeneral(""); setMode("choose"); return r;
                  })}>{t("addObservation")}</Button>
                  <Button variant="secondary" onClick={() => setMode("choose")}>{t("done")}</Button>
                </div>
              </Card>
            )}
            {mode === "dispute" && (
              <Card>
                <Field label={t("disagreeWhat")} hint={t("disagreeHint")}><Textarea rows={3} value={disputeReason} onChange={(e) => setDisputeReason(e.target.value)} /></Field>
                <div className="mt-2 flex flex-wrap gap-2">
                  <Button variant="danger" loading={busy === "dispute"} disabled={!disputeReason.trim()} onClick={() => act("dispute", () => post(`/api/tenant/inspections/${id}/dispute`, { comment: disputeReason }))}>{t("disputeReport")}</Button>
                  <Button variant="secondary" onClick={() => setMode("choose")}>{t("back")}</Button>
                </div>
              </Card>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
