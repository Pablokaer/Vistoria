"use client";

import { useParams, useRouter } from "next/navigation";
import { useCallback, useState } from "react";
import { BaselinePanel } from "@/components/ComparisonPanel";
import { PhotoUploader } from "@/components/PhotoUploader";
import { Badge, Button, Card, ErrorBanner, Loading, Notice, PageHeader } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { del, errorMessage, post, put } from "@/lib/api";
import { roomUrl, useInspectionRooms } from "@/lib/rooms";
import type { Defect, RoomDetail } from "@/lib/types";

type OnBusy = (key: string, busy: boolean) => void;

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

  const onBusy = useCallback<OnBusy>((key, b) => setUploading((u) => (u[key] === b ? u : { ...u, [key]: b })), []);

  if (!rooms || !inspection) return error ? <ErrorBanner message={error} onRetry={() => void load()} /> : <Loading />;

  const editable = rooms.some((r) => r.editable);
  const anyUploading = Object.values(uploading).some(Boolean);

  async function next() {
    setActionError(null);
    const missing = rooms!.flatMap((r) => [
      ...(r.generalPhotos.length === 0 ? [t("missingRoomPhoto", { room: r.name })] : []),
      ...r.defects.filter((d) => d.photos.length === 0).map((d) => t("missingDefectPhoto", { room: r.name, number: r.defects.indexOf(d) + 1 })),
    ]);
    setIssues(missing);
    if (missing.length > 0) { window.scrollTo({ top: 0, behavior: "smooth" }); return; }

    // Ask the API for every text that has not been written yet. Requests are idempotent per target.
    const requests = rooms!.flatMap((r) => {
      const base = roomUrl(id, r.id);
      return [
        ...(!r.finalDescription?.trim() ? [post(`${base}/analysis`)] : []),
        ...r.defects.filter((d) => !d.finalDescription?.trim()).map((d) => post(`${base}/defects/${d.id}/analysis`)),
        ...(r.comparison && !r.comparison.aiAnalysis ? [post(`${base}/comparison/analysis`)] : []),
      ];
    });
    setBusy(true);
    const results = await Promise.allSettled(requests);
    const failed = results.filter((x): x is PromiseRejectedResult => x.status === "rejected");
    if (failed.length > 0) {
      setActionError(t("aiRequestsFailed", { count: failed.length, error: errorMessage(failed[0].reason) }));
      setBusy(false);
      return;
    }
    router.push(`/agent/inspections/${id}/descriptions`);
  }

  return (
    <div className="pb-28">
      <PageHeader title={t("photosTitle")} subtitle={t("captureSubtitle", { type: humanize(inspection.inspectionType) })}
        back={{ href: `/agent/inspections/${id}`, label: t("inspection") }} />
      {!editable && <Notice tone="info">{t("readOnly", { status: humanize(inspection.status) })}</Notice>}
      <ErrorBanner message={actionError} />
      {issues.length > 0 && <ul className="mb-4 list-disc space-y-1 rounded-lg bg-red-50 py-3 pl-8 pr-4 text-sm text-red-800">{issues.map((i) => <li key={i}>{i}</li>)}</ul>}

      <div className="space-y-6">
        {rooms.map((r) => (
          <RoomCapture key={r.id} room={r} base={roomUrl(id, r.id)} onRoom={setRoom} reload={() => reloadRoom(r.id)} onBusy={onBusy} />
        ))}
      </div>

      {editable && (
        <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur">
          <div className="mx-auto flex max-w-6xl items-center justify-end gap-3 px-4 py-3">
            <span className="flex-1 text-xs text-slate-500">
              {anyUploading ? t("waitingUploads") : t("descriptionsGenerated")}
            </span>
            <Button size="lg" loading={busy} disabled={anyUploading} onClick={() => void next()}>{t("continueToDescriptions")}</Button>
          </div>
        </div>
      )}
    </div>
  );
}

function RoomCapture({ room, base, onRoom, reload, onBusy }: {
  room: RoomDetail; base: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void>; onBusy: OnBusy;
}) {
  const t = useT(captureMessages);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const onGeneralBusy = useCallback((b: boolean) => onBusy(`${room.id}:general`, b), [onBusy, room.id]);
  const photosBase = base.replace(/\/rooms\/[^/]+$/, "/photos");

  async function run(fn: () => Promise<void>) {
    setBusy(true);
    setError(null);
    try { await fn(); } catch (e) { setError(errorMessage(e)); } finally { setBusy(false); }
  }

  const addDefect = () => run(async () => onRoom(await post<RoomDetail>(`${base}/defects`, { description: null, location: null })));

  const removeDefect = (d: Defect) => {
    if (!confirm(t("confirmRemoveDefect"))) return;
    void run(async () => {
      let updated = await del<RoomDetail>(`${base}/defects/${d.id}`);
      // Removing the last defect leaves "defects found" set, which would block completing the room.
      if (updated.defects.length === 0 && updated.defectsFound)
        updated = await put<RoomDetail>(base, { finalDescription: updated.finalDescription, defectsFound: false, agentNotes: updated.agentNotes });
      onRoom(updated);
    });
  };

  return (
    <section id={`room-${room.id}`} className="scroll-mt-20">
      <Card title={<span className="flex flex-wrap items-center gap-3">{room.sequence}. {room.name} <Badge value={room.status} /></span>}>
        {room.comparison?.baseline && (
          <details className="mb-4">
            <summary className="cursor-pointer text-sm font-medium text-brand">{t("showMoveInRecord")}</summary>
            <div className="mt-2"><BaselinePanel room={room} /></div>
          </details>
        )}

        <PhotoUploader uploadUrl={`${base}/photos`} deleteUrlBase={photosBase} photos={room.generalPhotos} mediaType="General"
          editable={room.editable} onChanged={reload} onBusyChange={onGeneralBusy} label={t("roomPhotos")} />

        <div className="mt-6 border-t border-slate-100 pt-4">
          <h3 className="mb-3 text-sm font-semibold text-slate-700">{t("defectsCount", { count: room.defects.length })}</h3>
          <div className="space-y-3">
            {room.defects.map((d, i) => (
              <DefectCapture key={d.id} defect={d} index={i} base={base} editable={room.editable} reload={reload} onBusy={onBusy} onRemove={() => removeDefect(d)} />
            ))}
          </div>
          {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
          {room.editable && <Button variant="secondary" className="mt-3 w-full" loading={busy} onClick={() => void addDefect()}>{t("addDefect")}</Button>}
        </div>
      </Card>
    </section>
  );
}

function DefectCapture({ defect, index, base, editable, reload, onBusy, onRemove }: {
  defect: Defect; index: number; base: string; editable: boolean; reload: () => Promise<void>; onBusy: OnBusy; onRemove: () => void;
}) {
  const t = useT(captureMessages);
  const onDefectBusy = useCallback((b: boolean) => onBusy(`defect:${defect.id}`, b), [onBusy, defect.id]);
  return (
    <div data-testid="defect-card" className="rounded-xl border border-amber-200 bg-amber-50/40 p-3 sm:p-4">
      <div className="mb-2 flex items-center justify-between">
        <h4 className="font-medium">{t("defectNumber", { number: index + 1 })}</h4>
        {editable && <Button variant="ghost" onClick={onRemove}>{t("remove")}</Button>}
      </div>
      <PhotoUploader label={t("defectPhotos")} uploadUrl={`${base}/photos`} deleteUrlBase={base.replace(/\/rooms\/[^/]+$/, "/photos")}
        photos={defect.photos} mediaType="Defect" defectId={defect.id} editable={editable} onChanged={reload} onBusyChange={onDefectBusy} />
    </div>
  );
}
