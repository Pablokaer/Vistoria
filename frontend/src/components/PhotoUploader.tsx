"use client";

import { useEffect, useRef, useState } from "react";
import { del, errorMessage, uploadWithProgress } from "@/lib/api";
import { prepareImage } from "@/lib/image";
import type { Media } from "@/lib/types";
import { Button, Spinner } from "./ui";

interface QueueItem { id: string; name: string; progress: number; error?: string; file: File }

/**
 * Camera-first uploader: large buttons, sequential uploads with progress, retry on failure.
 * Photos are resized on the device before upload.
 */
export function PhotoUploader({ uploadUrl, deleteUrlBase, photos, mediaType, defectId, editable, onChanged, onBusyChange, label = "Photos" }: {
  uploadUrl: string; deleteUrlBase: string; photos: Media[]; mediaType: "General" | "Defect"; defectId?: string;
  editable: boolean; onChanged: () => void | Promise<void>; onBusyChange?: (busy: boolean) => void; label?: string;
}) {
  const cameraInput = useRef<HTMLInputElement>(null);
  const libraryInput = useRef<HTMLInputElement>(null);
  const [queue, setQueue] = useState<QueueItem[]>([]);
  const [viewing, setViewing] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const busy = queue.some((q) => !q.error);
  useEffect(() => { onBusyChange?.(busy); return () => onBusyChange?.(false); }, [busy, onBusyChange]);

  async function process(items: QueueItem[]) {
    for (const item of items) {
      try {
        const blob = await prepareImage(item.file);
        const form = new FormData();
        form.append("file", blob, item.file.name.replace(/\.(heic|heif|png|webp)$/i, ".jpg") || "photo.jpg");
        form.append("mediaType", mediaType);
        if (defectId) form.append("defectId", defectId);
        await uploadWithProgress<Media>(uploadUrl, form, (p) => setQueue((q) => q.map((x) => (x.id === item.id ? { ...x, progress: p } : x))));
        setQueue((q) => q.filter((x) => x.id !== item.id));
        await onChanged();
      } catch (e) {
        setQueue((q) => q.map((x) => (x.id === item.id ? { ...x, error: errorMessage(e) } : x)));
      }
    }
  }

  function onFiles(files: FileList | null) {
    if (!files?.length) return;
    const items = Array.from(files).map((file) => ({ id: crypto.randomUUID(), name: file.name, progress: 0, file }));
    setQueue((q) => [...q, ...items]);
    void process(items);
  }

  const retry = (item: QueueItem) => {
    setQueue((q) => q.map((x) => (x.id === item.id ? { ...x, error: undefined, progress: 0 } : x)));
    void process([{ ...item, error: undefined }]);
  };

  async function remove(photo: Media) {
    if (!confirm("Delete this photo?")) return;
    setDeleteError(null);
    try { await del(`${deleteUrlBase}/${photo.id}`); await onChanged(); } catch (e) { setDeleteError(errorMessage(e)); }
  }

  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <span className="text-sm font-medium text-slate-700">{label} ({photos.length})</span>
        {busy && <span className="flex items-center gap-2 text-xs text-slate-500"><Spinner small /> Uploading…</span>}
      </div>
      <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
        {photos.map((p) => (
          <div key={p.id} className="group relative aspect-square overflow-hidden rounded-lg bg-slate-100">
            {/* eslint-disable-next-line @next/next/no-img-element -- signed, short-lived URLs from the API */}
            <img src={p.url} alt={p.originalFilename} loading="lazy" className="h-full w-full cursor-zoom-in object-cover" onClick={() => setViewing(p.url)} />
            {editable && (
              <button onClick={() => void remove(p)} aria-label="Delete photo"
                className="absolute right-1 top-1 grid h-8 w-8 place-items-center rounded-full bg-black/60 text-white">✕</button>
            )}
          </div>
        ))}
        {queue.map((q) => (
          <div key={q.id} className={`flex aspect-square flex-col items-center justify-center gap-2 rounded-lg border p-2 text-center text-xs ${q.error ? "border-red-300 bg-red-50 text-red-700" : "border-slate-200 bg-white text-slate-500"}`}>
            {q.error ? (
              <>
                <span className="line-clamp-3">{q.error}</span>
                <button className="font-medium underline" onClick={() => retry(q)}>Retry</button>
                <button className="underline" onClick={() => setQueue((x) => x.filter((i) => i.id !== q.id))}>Dismiss</button>
              </>
            ) : (
              <>
                <Spinner small />
                <div className="h-1.5 w-full overflow-hidden rounded bg-slate-200"><div className="h-full bg-brand" style={{ width: `${Math.round(q.progress * 100)}%` }} /></div>
              </>
            )}
          </div>
        ))}
      </div>
      {deleteError && <p className="mt-2 text-sm text-red-600">{deleteError}</p>}
      {editable && (
        <div className="mt-3 grid grid-cols-2 gap-2">
          <Button type="button" size="lg" onClick={() => cameraInput.current?.click()}>📷 Take photo</Button>
          <Button type="button" size="lg" variant="secondary" onClick={() => libraryInput.current?.click()}>Upload photos</Button>
          <input ref={cameraInput} type="file" accept="image/*" capture="environment" className="hidden" onChange={(e) => { onFiles(e.target.files); e.target.value = ""; }} />
          <input ref={libraryInput} type="file" accept="image/jpeg,image/png,image/webp,image/*" multiple className="hidden" onChange={(e) => { onFiles(e.target.files); e.target.value = ""; }} />
        </div>
      )}
      {viewing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4" onClick={() => setViewing(null)}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={viewing} alt="" className="max-h-full max-w-full object-contain" />
        </div>
      )}
    </div>
  );
}

/** Read-only photo strip (baseline, reports). */
export function PhotoStrip({ urls }: { urls: string[] }) {
  const [viewing, setViewing] = useState<string | null>(null);
  if (urls.length === 0) return <p className="text-sm text-slate-500">No photos.</p>;
  return (
    <>
      <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
        {urls.map((u) => (
          // eslint-disable-next-line @next/next/no-img-element
          <img key={u} src={u} alt="" loading="lazy" className="aspect-square w-full cursor-zoom-in rounded-lg bg-slate-100 object-cover" onClick={() => setViewing(u)} />
        ))}
      </div>
      {viewing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4" onClick={() => setViewing(null)}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={viewing} alt="" className="max-h-full max-w-full object-contain" />
        </div>
      )}
    </>
  );
}
