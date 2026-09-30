"use client";

import { useParams } from "next/navigation";
import { useState } from "react";
import { ReportBody, ReportMeta, ReportToolbar } from "@/components/ReportView";
import { ReportPageSkeleton } from "@/components/report/ReportPageSkeleton";
import { RoomObservation } from "@/components/tenant/RoomObservation";
import { TenantResponsePanel } from "@/components/tenant/TenantResponsePanel";
import { ErrorBanner, Notice, useConfirm, useToast, type ToastTone } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages, type TenantKey } from "@/i18n/messages/tenant";
import { errorMessage, post } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { ReportView } from "@/lib/types";

/** Runs one tenant action, keeps the returned report and reports the outcome as a toast. */
function useTenantAction(setData: (r: ReportView) => void) {
  const t = useT(tenantMessages);
  const toast = useToast();
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  async function act(name: string, call: () => Promise<ReportView>, done: { key: TenantKey; tone: ToastTone }): Promise<boolean> {
    setBusy(name);
    setError(null);
    try {
      setData(await call());
      toast.notify({ tone: done.tone, title: t(done.key) });
      return true;
    } catch (e) { setError(errorMessage(e)); return false; } finally { setBusy(null); }
  }
  return { busy, error, act };
}

export default function TenantReviewPage() {
  const { id } = useParams<{ id: string }>();
  const t = useT(tenantMessages);
  const { data, setData, error, loading, reload } = useApi<ReportView>(`/api/tenant/inspections/${id}/report`);
  const { busy, error: actionError, act } = useTenantAction(setData);
  const [confirm, confirmDialog] = useConfirm();
  const base = `/api/tenant/inspections/${id}`;

  if (loading && !data) return <ReportPageSkeleton />;
  if (!data) return <ErrorBanner message={error ?? t("reportNotFound")} onRetry={reload} />;
  const open = data.canRespond;
  const observe = (roomId: string | null, text: string) =>
    act(roomId ? "room" : "general", () => post<ReportView>(`${base}/observations`, roomId ? { roomId, text } : { text }), { key: "observationAdded", tone: "success" });

  async function accept() {
    const ok = await confirm({ title: t("confirmAcceptTitle"), description: t("confirmAcceptText"), confirmLabel: t("confirmAcceptButton") });
    if (ok) await act("accept", () => post(`${base}/accept`, { comment: t("everythingCorrect") }), { key: "reportAcceptedToast", tone: "success" });
  }

  return (
    <div className={open ? "pb-64 xl:pb-0" : ""}>
      {confirmDialog}
      <ReportToolbar report={data} backHref="/tenant" backLabel={t("myInspections")} />
      <ErrorBanner message={actionError} />
      {!open && (
        <Notice tone={data.inspectionStatus === "Disputed" ? "warning" : "success"}>
          {data.inspectionStatus === "Accepted" ? t("youAccepted") : t("youDisputed")}
        </Notice>
      )}
      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_18rem]">
        <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} observations={data.observations}
          renderRoomExtra={(roomId) => open && <RoomObservation busy={busy === "room"} onSubmit={(text) => observe(roomId, text)} />} />
        <aside className="space-y-4 xl:sticky xl:top-6">
          {open && (
            <TenantResponsePanel busy={busy} onAccept={() => void accept()} onGeneralObservation={(text) => observe(null, text)}
              onDispute={(reason) => void act("dispute", () => post(`${base}/dispute`, { comment: reason }), { key: "reportDisputedToast", tone: "info" })} />
          )}
          <ReportMeta report={data} showContents={!open} />
        </aside>
      </div>
    </div>
  );
}
