"use client";

import { useEffect, useRef, useState } from "react";
import { Lightbox, PhotoGallery } from "@/components/inspection/PhotoGallery";
import { CaptureButtons, CaptureZone, PhotoThumb, QueueTile, type QueueItem } from "@/components/inspection/UploadTiles";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { del, errorMessage, uploadWithProgress } from "@/lib/api";
import { cx } from "@/lib/cx";
import { prepareImage } from "@/lib/image";
import type { Media } from "@/lib/types";
import { Spinner, useConfirm, useToast } from "./ui";

interface UploaderProps {
  uploadUrl: string; deleteUrlBase: string; photos: Media[]; mediaType: "General" | "Defect"; defectId?: string;
  editable: boolean; onChanged: () => void | Promise<void>; onBusyChange?: (busy: boolean) => void; label?: string;
  /** Smaller layout for defect photos inside a room. */
  compact?: boolean;
}

/** Sequential upload of picked files: resize on the device, upload with progress, keep failures for retry. */
function useUploadQueue({ uploadUrl, mediaType, defectId, onChanged }: Pick<UploaderProps, "uploadUrl" | "mediaType" | "defectId" | "onChanged">) {
  const t = useT(inspectionToolsMessages);
  const toast = useToast();
  const [queue, setQueue] = useState<QueueItem[]>([]);
  const patch = (id: string, change: Partial<QueueItem>) => setQueue((q) => q.map((x) => (x.id === id ? { ...x, ...change } : x)));

  async function uploadOne(item: QueueItem): Promise<boolean> {
    try {
      const blob = await prepareImage(item.file);
      patch(item.id, { stage: "uploading" });
      const form = new FormData();
      form.append("file", blob, item.file.name.replace(/\.(heic|heif|png|webp)$/i, ".jpg") || "photo.jpg");
      form.append("mediaType", mediaType);
      if (defectId) form.append("defectId", defectId);
      await uploadWithProgress<Media>(uploadUrl, form, (p) => patch(item.id, { progress: p }));
      setQueue((q) => q.filter((x) => x.id !== item.id));
      await onChanged();
      return true;
    } catch (e) {
      patch(item.id, { error: errorMessage(e) });
      return false;
    }
  }

  async function process(items: QueueItem[]) {
    let uploaded = 0;
    for (const item of items) if (await uploadOne(item)) uploaded++;
    if (uploaded > 0) toast.notify({ tone: "success", title: t("photosUploaded", { count: uploaded }) });
  }

  const add = (files: FileList) => {
    const items: QueueItem[] = Array.from(files).map((file) => ({ id: crypto.randomUUID(), name: file.name, progress: 0, stage: "preparing", file }));
    setQueue((q) => [...q, ...items]);
    void process(items);
  };
  const retry = (item: QueueItem) => {
    const fresh = { ...item, error: undefined, progress: 0, stage: "preparing" as const };
    patch(item.id, fresh);
    void process([fresh]);
  };
  const dismiss = (id: string) => setQueue((q) => q.filter((x) => x.id !== id));
  return { queue, add, retry, dismiss, busy: queue.some((q) => !q.error) };
}

/**
 * Camera-first uploader: an empty capture area before the first photo, then a thumbnail grid with delete,
 * per-file progress and retry. Photos are resized on the device before upload.
 */
export function PhotoUploader(props: UploaderProps) {
  const { deleteUrlBase, photos, editable, onChanged, onBusyChange, label, compact } = props;
  const t = useT(inspectionToolsMessages);
  const [confirm, confirmDialog] = useConfirm();
  const cameraInput = useRef<HTMLInputElement>(null);
  const libraryInput = useRef<HTMLInputElement>(null);
  const { queue, add, retry, dismiss, busy } = useUploadQueue(props);
  const [viewing, setViewing] = useState<number | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  useEffect(() => { onBusyChange?.(busy); return () => onBusyChange?.(false); }, [busy, onBusyChange]);

  async function remove(photo: Media) {
    if (!(await confirm({ title: t("confirmDeletePhoto"), confirmLabel: t("deletePhoto"), danger: true }))) return;
    setDeleteError(null);
    try { await del(`${deleteUrlBase}/${photo.id}`); await onChanged(); } catch (e) { setDeleteError(errorMessage(e)); }
  }

  const empty = photos.length === 0 && queue.length === 0;
  const buttons = <CaptureButtons compact={compact} onCamera={() => cameraInput.current?.click()} onLibrary={() => libraryInput.current?.click()} />;
  return (
    <div>
      <div className="mb-2 flex items-center justify-between gap-2">
        <span className="text-label font-medium text-ink-2">{label ?? t("photosDefault")} <span className="tabular text-ink-3">({photos.length})</span></span>
        {busy && <span className="flex items-center gap-2 text-caption text-ink-3"><Spinner small /> {t("uploading")}</span>}
      </div>
      {empty && editable ? (
        <CaptureZone compact={compact} hint={props.mediaType === "Defect" ? t("captureHintDefect") : t("captureHintRoom")} onFiles={add}>{buttons}</CaptureZone>
      ) : (
        <>
          {empty && <p className="text-label text-ink-3">{t("noPhotos")}</p>}
          <div className={cx("grid gap-2", compact ? "grid-cols-4 sm:grid-cols-6" : "grid-cols-3 sm:grid-cols-4 xl:grid-cols-5")}>
            {photos.map((p, i) => <PhotoThumb key={p.id} photo={p} number={i + 1} editable={editable} onOpen={() => setViewing(i)} onDelete={() => void remove(p)} />)}
            {queue.map((q) => <QueueTile key={q.id} item={q} onRetry={() => retry(q)} onDismiss={() => dismiss(q.id)} />)}
          </div>
          {editable && <div className="mt-3">{buttons}</div>}
        </>
      )}
      {deleteError && <p role="alert" className="mt-2 text-label text-danger-700">{deleteError}</p>}
      <input ref={cameraInput} type="file" accept="image/*" capture="environment" className="hidden" onChange={(e) => { if (e.target.files) add(e.target.files); e.target.value = ""; }} />
      <input ref={libraryInput} type="file" accept="image/jpeg,image/png,image/webp,image/*" multiple className="hidden" onChange={(e) => { if (e.target.files) add(e.target.files); e.target.value = ""; }} />
      {viewing !== null && <Lightbox urls={photos.map((p) => p.url)} index={viewing} onIndex={setViewing} onClose={() => setViewing(null)} />}
      {confirmDialog}
    </div>
  );
}

/** Read-only photo strip (baseline, reports). Kept as the public name used by the report view. */
export function PhotoStrip({ urls }: { urls: string[] }) {
  return <PhotoGallery urls={urls} />;
}
