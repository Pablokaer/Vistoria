"use client";

import { Icon } from "@/components/icons";
import { Button } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { cx } from "@/lib/cx";
import type { ReportSnapshot } from "@/lib/types";

function Counts({ preview }: { preview: ReportSnapshot }) {
  const t = useT(captureMessages);
  const photos = preview.rooms.reduce((n, r) => n + r.photos.length + r.defects.reduce((m, d) => m + d.photos.length, 0), 0);
  const defects = preview.rooms.reduce((n, r) => n + r.defects.length, 0);
  const items = [[t("summaryRooms"), preview.rooms.length], [t("summaryPhotos"), photos], [t("summaryDefects"), defects]] as const;
  return (
    <dl className="hidden grid-cols-3 gap-2 lg:grid">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-md bg-surface-2 px-2 py-1.5 text-center ring-1 ring-inset ring-line">
          <dd className="tabular text-section font-semibold text-ink">{value}</dd>
          <dt className="text-caption text-ink-3">{label}</dt>
        </div>
      ))}
    </dl>
  );
}

/**
 * The finalize decision, kept apart from the document: readiness, what finalizing means and the action.
 * One element: a fixed bar on phones, a sticky side panel on desktop.
 */
export function FinalizePanel({ preview, blocking, canFinalize, busy, onFinalize }: {
  preview: ReportSnapshot; blocking: number; canFinalize: boolean; busy: boolean; onFinalize: () => void;
}) {
  const t = useT(captureMessages);
  const ready = blocking === 0 && canFinalize;
  return (
    <aside className="fixed inset-x-0 bottom-[calc(3.5rem+env(safe-area-inset-bottom))] z-30 space-y-3 border-t border-line bg-surface/95 px-4 py-3 shadow-overlay backdrop-blur
      lg:sticky lg:bottom-auto lg:top-6 lg:rounded-lg lg:border lg:p-4 lg:shadow-card">
      <p className={cx("flex items-center gap-2 text-label font-semibold", ready ? "text-success-700" : "text-warning-700")}>
        <Icon name={ready ? "checkCircle" : "alert"} className="h-5 w-5 shrink-0" />
        {ready ? t("readyToFinalize") : t("issuesToFix", { count: blocking })}
      </p>
      <Counts preview={preview} />
      <p className="hidden text-label text-ink-3 lg:block">{t("finalizeHint")}</p>
      <Button size="lg" icon="lock" className="w-full" disabled={!canFinalize} loading={busy} onClick={onFinalize}>{t("finalizeInspection")}</Button>
    </aside>
  );
}
