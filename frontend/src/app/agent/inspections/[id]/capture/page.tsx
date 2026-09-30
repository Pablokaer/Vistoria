"use client";

import { useParams, useRouter } from "next/navigation";
import { useCallback, useState } from "react";
import { CaptureRoom, type OnBusy } from "@/components/inspection/CaptureRoom";
import { ExecutionBar, RoomStepper } from "@/components/inspection/ExecutionBar";
import { ExecutionLayout, IssueList } from "@/components/inspection/ExecutionLayout";
import { RoomChipStrip, useCurrentRoom, type RoomNavItem } from "@/components/inspection/RoomNavigation";
import { StepHeader } from "@/components/inspection/StepHeader";
import { Button, ErrorBanner, Notice, PageSkeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import type { Translate } from "@/i18n/translate";
import { errorMessage, post } from "@/lib/api";
import { roomPhotosReady, roomUrl, useInspectionRooms } from "@/lib/rooms";
import type { RoomDetail } from "@/lib/types";

type CaptureKey = keyof (typeof captureMessages)["en"];

/** Every room and defect that still needs a photo before the AI can describe it. */
function missingPhotos(rooms: RoomDetail[], t: Translate<CaptureKey>): string[] {
  return rooms.flatMap((r) => [
    ...(r.generalPhotos.length === 0 ? [t("missingRoomPhoto", { room: r.name })] : []),
    ...r.defects.filter((d) => d.photos.length === 0).map((d) => t("missingDefectPhoto", { room: r.name, number: r.defects.indexOf(d) + 1 })),
  ]);
}

/** Asks the API for every text that has not been written yet. Requests are idempotent per target. */
function requestMissingTexts(inspectionId: string, rooms: RoomDetail[]): Promise<unknown>[] {
  return rooms.flatMap((r) => {
    const base = roomUrl(inspectionId, r.id);
    return [
      ...(!r.finalDescription?.trim() ? [post(`${base}/analysis`)] : []),
      ...r.defects.filter((d) => !d.finalDescription?.trim()).map((d) => post(`${base}/defects/${d.id}/analysis`)),
      ...(r.comparison && !r.comparison.aiAnalysis ? [post(`${base}/comparison/analysis`)] : []),
    ];
  });
}

const navItems = (rooms: RoomDetail[]): RoomNavItem[] =>
  rooms.map((r) => ({ id: r.id, name: r.name, sequence: r.sequence, state: roomPhotosReady(r) ? "ready" : "pending" }));

/**
 * Step 1 of the inspection: photos only. Every room is listed in sequence with its general photos and
 * its defects' photos. No text is written here — "Continue" requests every AI description at once.
 */
export default function CapturePage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const t = useT(captureMessages);
  const { humanize } = useFormatters();
  const { inspection, rooms, error, load, setRoom, reloadRoom } = useInspectionRooms(id);
  const [uploading, setUploading] = useState<Record<string, boolean>>({});
  const [busy, setBusy] = useState(false);
  const [issues, setIssues] = useState<string[]>([]);
  const [actionError, setActionError] = useState<string | null>(null);
  const currentId = useCurrentRoom(rooms?.map((r) => r.id) ?? []);
  const onBusy = useCallback<OnBusy>((key, b) => setUploading((u) => (u[key] === b ? u : { ...u, [key]: b })), []);

  if (!rooms || !inspection) return error ? <ErrorBanner message={error} onRetry={() => void load()} /> : <PageSkeleton />;

  const editable = rooms.some((r) => r.editable);
  const anyUploading = Object.values(uploading).some(Boolean);
  const nav = navItems(rooms);
  const ready = nav.filter((n) => n.state === "ready").length;

  async function next() {
    setActionError(null);
    const missing = missingPhotos(rooms!, t);
    setIssues(missing);
    if (missing.length > 0) { window.scrollTo({ top: 0, behavior: "smooth" }); return; }
    setBusy(true);
    const failed = (await Promise.allSettled(requestMissingTexts(id, rooms!))).filter((x): x is PromiseRejectedResult => x.status === "rejected");
    if (failed.length > 0) {
      setActionError(t("aiRequestsFailed", { count: failed.length, error: errorMessage(failed[0].reason) }));
      setBusy(false);
      return;
    }
    router.push(`/agent/inspections/${id}/descriptions`);
  }

  return (
    <>
      <StepHeader inspection={inspection} step={1} title={t("photosTitle")}
        progress={{ value: ready, total: rooms.length, label: t("progressPhotographed", { value: ready, total: rooms.length }) }}
        rooms={<RoomChipStrip items={nav} currentId={currentId} />} />
      {!editable && <Notice tone="info">{t("readOnly", { status: humanize(inspection.status) })}</Notice>}
      <ExecutionLayout nav={nav} currentId={currentId}>
        <ErrorBanner message={actionError} />
        <IssueList issues={issues} />
        {rooms.map((r) => (
          <CaptureRoom key={r.id} room={r} base={roomUrl(id, r.id)} onRoom={setRoom} reload={() => reloadRoom(r.id)} onBusy={onBusy} />
        ))}
      </ExecutionLayout>
      {editable && (
        <ExecutionBar stepper={<RoomStepper items={nav} currentId={currentId} />} hint={anyUploading ? t("waitingUploads") : t("descriptionsGenerated")}>
          <Button size="lg" icon="arrowRight" loading={busy} disabled={anyUploading} onClick={() => void next()}>{t("continueToDescriptions")}</Button>
        </ExecutionBar>
      )}
    </>
  );
}
