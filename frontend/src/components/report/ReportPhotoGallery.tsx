"use client";

import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import { cx } from "@/lib/cx";

/**
 * Photo grid of a report room/defect with a keyboard-friendly lightbox (Esc closes, arrows browse).
 * Report-specific on purpose: the capture screens have their own upload-oriented strip.
 * Example: `<ReportPhotoGallery urls={urls} size="sm" />`
 */
export function ReportPhotoGallery({ urls, size = "md" }: { urls: string[]; size?: "sm" | "md" }) {
  const t = useT(reportMessages);
  const [index, setIndex] = useState<number | null>(null);
  if (urls.length === 0) return <p className="text-label text-ink-4">{t("noPhotos")}</p>;
  return (
    <>
      <ul className={cx("grid gap-2", size === "sm" ? "grid-cols-4 sm:grid-cols-6" : "grid-cols-2 sm:grid-cols-3 lg:grid-cols-4")}>
        {urls.map((url, i) => (
          <li key={url}>
            <button type="button" onClick={() => setIndex(i)} aria-label={t("openPhoto", { index: i + 1, total: urls.length })}
              className="group block aspect-[4/3] w-full overflow-hidden rounded-md bg-neutral-50 ring-1 ring-inset ring-line">
              {/* eslint-disable-next-line @next/next/no-img-element -- signed storage URLs; next/image cannot optimise them */}
              <img src={url} alt="" loading="lazy" className="h-full w-full object-cover transition duration-300 group-hover:scale-[1.03]" />
            </button>
          </li>
        ))}
      </ul>
      {index !== null && <Lightbox urls={urls} index={index} onChange={setIndex} onClose={() => setIndex(null)} />}
    </>
  );
}

function Lightbox({ urls, index, onChange, onClose }: { urls: string[]; index: number; onChange: (i: number) => void; onClose: () => void }) {
  const t = useT(reportMessages);
  const step = useCallback((delta: number) => onChange((index + delta + urls.length) % urls.length), [index, onChange, urls.length]);
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowRight") step(1);
      if (e.key === "ArrowLeft") step(-1);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [onClose, step]);
  const control = "grid h-11 w-11 place-items-center rounded-full bg-white/10 text-white hover:bg-white/20";
  return (
    <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 flex items-center justify-center bg-ink/90 p-4 animate-fade-in" onClick={onClose}>
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={urls[index]} alt="" className="max-h-full max-w-full rounded-md object-contain" onClick={(e) => e.stopPropagation()} />
      <button type="button" aria-label={t("closePhoto")} onClick={onClose} className={cx(control, "absolute right-4 top-4")}><Icon name="close" className="h-5 w-5" /></button>
      {urls.length > 1 && (
        <div className="absolute inset-x-0 bottom-6 flex items-center justify-center gap-4" onClick={(e) => e.stopPropagation()}>
          <button type="button" aria-label={t("previousPhoto")} onClick={() => step(-1)} className={control}><Icon name="chevronLeft" className="h-5 w-5" /></button>
          <span className="tabular text-label text-white/80">{index + 1} / {urls.length}</span>
          <button type="button" aria-label={t("nextPhoto")} onClick={() => step(1)} className={control}><Icon name="chevronRight" className="h-5 w-5" /></button>
        </div>
      )}
    </div>
  );
}
