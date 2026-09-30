"use client";

import { useEffect, useState } from "react";
import { Icon } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { cx } from "@/lib/cx";

/**
 * Full-screen photo viewer with previous/next (buttons and arrow keys) and Esc to close.
 * Example: `{index !== null && <Lightbox urls={urls} index={index} onIndex={setIndex} onClose={() => setIndex(null)} />}`
 */
export function Lightbox({ urls, index, onIndex, onClose }: { urls: string[]; index: number; onIndex: (i: number) => void; onClose: () => void }) {
  const t = useT(inspectionToolsMessages);
  const step = (delta: number) => onIndex((index + delta + urls.length) % urls.length);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowRight") onIndex((index + 1) % urls.length);
      if (e.key === "ArrowLeft") onIndex((index - 1 + urls.length) % urls.length);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [index, urls.length, onIndex, onClose]);

  const control = "grid h-11 w-11 place-items-center rounded-full bg-white/10 text-white transition hover:bg-white/20";
  return (
    <div role="dialog" aria-modal="true" aria-label={t("photoViewer")} className="fixed inset-0 z-50 flex items-center justify-center bg-black/90 p-4 animate-fade-in" onClick={onClose}>
      {/* eslint-disable-next-line @next/next/no-img-element -- signed, short-lived URLs from the API */}
      <img src={urls[index]} alt="" className="max-h-full max-w-full object-contain" onClick={(e) => e.stopPropagation()} />
      <button type="button" aria-label={t("closePhoto")} className={cx(control, "absolute right-4 top-4")} onClick={onClose}><Icon name="close" className="h-5 w-5" /></button>
      {urls.length > 1 && (
        <>
          <button type="button" aria-label={t("previousPhoto")} className={cx(control, "absolute left-4 top-1/2 -translate-y-1/2")} onClick={(e) => { e.stopPropagation(); step(-1); }}><Icon name="chevronLeft" className="h-6 w-6" /></button>
          <button type="button" aria-label={t("nextPhoto")} className={cx(control, "absolute right-4 top-1/2 -translate-y-1/2")} onClick={(e) => { e.stopPropagation(); step(1); }}><Icon name="chevronRight" className="h-6 w-6" /></button>
          <span className="tabular absolute bottom-5 left-1/2 -translate-x-1/2 rounded-full bg-white/10 px-3 py-1 text-label text-white">{index + 1} / {urls.length}</span>
        </>
      )}
    </div>
  );
}

const GRID = { md: "grid-cols-3 sm:grid-cols-4", sm: "grid-cols-4 sm:grid-cols-6" };

/** Read-only thumbnails that open the lightbox (baseline, reports, descriptions step). */
export function PhotoGallery({ urls, size = "md", emptyText }: { urls: string[]; size?: "sm" | "md"; emptyText?: string }) {
  const t = useT(inspectionToolsMessages);
  const [viewing, setViewing] = useState<number | null>(null);
  if (urls.length === 0) return <p className="text-label text-ink-3">{emptyText ?? t("noPhotos")}</p>;
  return (
    <>
      <div className={cx("grid gap-2", GRID[size])}>
        {urls.map((u, i) => (
          <button key={u} type="button" onClick={() => setViewing(i)} aria-label={t("openPhoto", { number: i + 1 })}
            className="group relative aspect-square overflow-hidden rounded-md bg-neutral-50 ring-1 ring-inset ring-line">
            {/* eslint-disable-next-line @next/next/no-img-element -- signed, short-lived URLs from the API */}
            <img src={u} alt="" loading="lazy" className="h-full w-full object-cover transition duration-200 group-hover:scale-[1.03]" />
          </button>
        ))}
      </div>
      {viewing !== null && <Lightbox urls={urls} index={viewing} onIndex={setViewing} onClose={() => setViewing(null)} />}
    </>
  );
}
