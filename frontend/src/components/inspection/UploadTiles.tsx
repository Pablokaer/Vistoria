"use client";

import { useEffect, useState, type DragEvent, type ReactNode } from "react";
import { Icon } from "@/components/icons";
import { Button } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { cx } from "@/lib/cx";
import type { Media } from "@/lib/types";

/** A photo waiting for, or failing, upload. `stage` separates on-device resizing from the network upload. */
export interface QueueItem { id: string; name: string; progress: number; stage: "preparing" | "uploading"; error?: string; file: File }

/** Local preview of a picked file (object URL, revoked on unmount). Not an <img>, so it never counts as an uploaded photo. */
function useObjectUrl(file: File): string | null {
  const [url, setUrl] = useState<string | null>(null);
  useEffect(() => {
    const next = URL.createObjectURL(file);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- the URL must be created and revoked with the effect.
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [file]);
  return url;
}

/** Upload in progress or failed: preview dimmed behind progress, or an error with retry/dismiss. */
export function QueueTile({ item, onRetry, onDismiss }: { item: QueueItem; onRetry: () => void; onDismiss: () => void }) {
  const t = useT(inspectionToolsMessages);
  const preview = useObjectUrl(item.file);
  const pct = Math.round(item.progress * 100);
  return (
    <div className="relative aspect-square overflow-hidden rounded-md bg-neutral-50 bg-cover bg-center ring-1 ring-inset ring-line"
      style={preview ? { backgroundImage: `url(${preview})` } : undefined}>
      {item.error ? (
        <div role="alert" className="absolute inset-0 flex flex-col items-center justify-center gap-1.5 bg-danger-50/95 p-2 text-center text-caption text-danger-700">
          <Icon name="alert" className="h-5 w-5" />
          <span className="line-clamp-2">{item.error}</span>
          <div className="flex gap-3 font-medium">
            <button type="button" className="underline" onClick={onRetry}>{t("retry")}</button>
            <button type="button" className="underline" onClick={onDismiss}>{t("dismiss")}</button>
          </div>
        </div>
      ) : (
        <div className="absolute inset-0 flex flex-col justify-end bg-ink/45 p-2 text-white">
          <span className="mb-1 text-caption font-medium">{item.stage === "preparing" ? t("preparing") : `${t("uploading")} ${pct}%`}</span>
          <div className="h-1 overflow-hidden rounded-full bg-white/30"><div className="h-full rounded-full bg-white transition-[width] duration-300" style={{ width: `${item.stage === "preparing" ? 8 : pct}%` }} /></div>
        </div>
      )}
    </div>
  );
}

/** Uploaded photo: tap to enlarge, corner button to delete. */
export function PhotoThumb({ photo, number, editable, onOpen, onDelete }: { photo: Media; number: number; editable: boolean; onOpen: () => void; onDelete: () => void }) {
  const t = useT(inspectionToolsMessages);
  return (
    <div className="group relative aspect-square overflow-hidden rounded-md bg-neutral-50 ring-1 ring-inset ring-line animate-fade-in">
      <button type="button" onClick={onOpen} aria-label={t("openPhoto", { number })} className="block h-full w-full">
        {/* eslint-disable-next-line @next/next/no-img-element -- signed, short-lived URLs from the API */}
        <img src={photo.url} alt={photo.originalFilename} loading="lazy" className="h-full w-full object-cover transition duration-200 group-hover:scale-[1.03]" />
      </button>
      {editable && (
        <button type="button" onClick={onDelete} aria-label={t("deletePhoto")}
          className="absolute right-1 top-1 grid h-8 w-8 place-items-center rounded-full bg-ink/65 text-white transition hover:bg-danger-700">
          <Icon name="trash" className="h-4 w-4" />
        </button>
      )}
    </div>
  );
}

/**
 * Camera-first actions. "Take photo" opens the rear camera on phones; "Upload photos" opens the library.
 * Phones: the camera gets the full width (it is what the inspector uses walking the property). From sm up the
 * two sit side by side at a natural width instead of stretching across the room card.
 */
export function CaptureButtons({ onCamera, onLibrary, compact }: { onCamera: () => void; onLibrary: () => void; compact?: boolean }) {
  const t = useT(inspectionToolsMessages);
  return (
    <div className={cx("grid gap-2 sm:flex sm:flex-wrap", compact ? "grid-cols-2" : "grid-cols-1")}>
      <Button type="button" size={compact ? "md" : "lg"} icon="camera" className="whitespace-nowrap sm:min-w-44" onClick={onCamera}>{t("takePhoto")}</Button>
      <Button type="button" size="md" variant="secondary" icon="upload" className={cx("whitespace-nowrap", !compact && "sm:min-h-12")} onClick={onLibrary}>{t("uploadPhotos")}</Button>
    </div>
  );
}

/**
 * Empty capture area shown before the first photo: what to photograph + the two capture actions.
 * Accepts drag-and-drop on desktop.
 */
export function CaptureZone({ hint, compact, onFiles, children }: { hint: string; compact?: boolean; onFiles: (files: FileList) => void; children: ReactNode }) {
  const t = useT(inspectionToolsMessages);
  const [over, setOver] = useState(false);
  const drop = (e: DragEvent) => { e.preventDefault(); setOver(false); if (e.dataTransfer.files.length) onFiles(e.dataTransfer.files); };
  return (
    <div onDragOver={(e) => { e.preventDefault(); setOver(true); }} onDragLeave={() => setOver(false)} onDrop={drop}
      className={cx("rounded-lg border border-dashed text-center transition", compact ? "p-3" : "px-4 py-6", over ? "border-brand bg-brand-50" : "border-line-strong bg-surface-2")}>
      {!compact && <span className="mx-auto mb-2 grid h-11 w-11 place-items-center rounded-full bg-surface text-brand shadow-card"><Icon name="camera" className="h-5 w-5" /></span>}
      <p className={cx("mx-auto max-w-sm text-ink-3", compact ? "mb-2 text-caption" : "mb-4 text-label")}>{hint}</p>
      <div className="sm:[&>div]:justify-center">{children}</div>
      <p className="mt-2 hidden text-caption text-ink-4 lg:block">{t("dropHere")}</p>
    </div>
  );
}
