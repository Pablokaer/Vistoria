"use client";

import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { FinalizePanel } from "@/components/inspection/FinalizePanel";
import { IssueList } from "@/components/inspection/ExecutionLayout";
import { StepHeader } from "@/components/inspection/StepHeader";
import { ReportBody } from "@/components/ReportView";
import { Button, ErrorBanner, LinkButton, Notice, PageSkeleton, Textarea, useConfirm, useToast } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { errorMessage, post, put } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { Review } from "@/lib/types";

/** Inline editor of one room's final text inside the preview (Review status only). */
function RoomTextEdit({ review, roomId, inspectionId, onSaved }: { review: Review; roomId: string; inspectionId: string; onSaved: () => Promise<void> }) {
  const t = useT(captureMessages);
  const current = review.preview.rooms.find((r) => r.id === roomId);
  const [editing, setEditing] = useState(false);
  const [text, setText] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  if (!current) return null;
  async function save() {
    setBusy(true);
    setError(null);
    try {
      await put(`/api/agent/inspections/${inspectionId}/rooms/${roomId}`, { finalDescription: text, defectsFound: current!.defectsFound, agentNotes: current!.agentNotes });
      setEditing(false);
      await onSaved();
    } catch (e) { setError(errorMessage(e)); } finally { setBusy(false); }
  }
  if (!editing) {
    return (
      <div className="mt-3 flex flex-wrap gap-2 border-t border-line pt-3">
        <Button variant="subtle" size="sm" icon="edit" onClick={() => { setEditing(true); setText(current.description ?? ""); }}>{t("editDescription")}</Button>
        <LinkButton variant="subtle" size="sm" icon="arrowRight" href={`/agent/inspections/${inspectionId}/descriptions#room-${roomId}`}>{t("openRoom")}</LinkButton>
      </div>
    );
  }
  return (
    <div className="mt-3 space-y-2 border-t border-line pt-3">
      <Textarea rows={5} value={text} onChange={(e) => setText(e.target.value)} />
      {error && <p role="alert" className="text-label text-danger-700">{error}</p>}
      <div className="flex gap-2">
        <Button size="sm" loading={busy} onClick={() => void save()}>{t("saveText")}</Button>
        <Button size="sm" variant="secondary" onClick={() => setEditing(false)}>{t("cancel")}</Button>
      </div>
    </div>
  );
}

export default function ReviewPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const t = useT(captureMessages);
  const toast = useToast();
  const [confirm, confirmDialog] = useConfirm();
  const { data, error, loading, reload } = useApi<Review>(`/api/agent/inspections/${id}/review`);
  const [busy, setBusy] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setActionError(null);
    try { await fn(); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  }

  if (loading && !data) return <PageSkeleton />;
  if (!data) return <ErrorBanner message={error} onRetry={reload} />;
  const inReview = data.inspection.status === "Review";
  const total = data.inspection.rooms.length;

  const backToRooms = () => act("back", async () => { await post(`/api/agent/inspections/${id}/return-to-progress`); router.push(`/agent/inspections/${id}`); });
  async function finalize() {
    if (!(await confirm({ title: t("confirmFinalizeTitle"), description: t("finalizeHint"), confirmLabel: t("confirmFinalizeButton") }))) return;
    await act("finalize", async () => {
      const res = await post<{ reportId: string }>(`/api/agent/inspections/${id}/finalize`);
      toast.notify({ tone: "success", title: t("reportFinalized") });
      router.replace(`/reports/${res.reportId}`);
    });
  }

  return (
    <>
      <StepHeader inspection={data.inspection} step={3} title={t("reviewTitle")}
        progress={{ value: data.inspection.roomsCompleted, total, label: t("progressCompleted", { value: data.inspection.roomsCompleted, total }) }}
        actions={inReview && <Button variant="secondary" size="sm" icon="arrowLeft" loading={busy === "back"} onClick={() => void backToRooms()}>{t("backToRooms")}</Button>} />
      <p className="-mt-2 mb-4 max-w-2xl text-label text-ink-3">{t("reviewSubtitle")}</p>
      <ErrorBanner message={actionError} />
      {!inReview && data.inspection.status === "InProgress" && <Notice tone="warning">{t("submitFirst")}</Notice>}
      <div className="grid gap-6 pb-40 lg:grid-cols-[minmax(0,1fr)_17rem] lg:items-start lg:pb-8">
        <div className="min-w-0 space-y-4">
          <IssueList title={t("finalizationBlocked")} issues={data.blockingIssues} />
          <div className="rounded-xl bg-surface-2 p-2 ring-1 ring-inset ring-line sm:p-4">
            <ReportBody snapshot={data.preview} photoUrls={data.photoUrls} embedded
              renderRoomExtra={(roomId) => inReview && <RoomTextEdit review={data} roomId={roomId} inspectionId={id} onSaved={reload} />} />
          </div>
        </div>
        {inReview && <FinalizePanel preview={data.preview} blocking={data.blockingIssues.length} canFinalize={data.canFinalize} busy={busy === "finalize"} onFinalize={() => void finalize()} />}
      </div>
      {confirmDialog}
    </>
  );
}
