"use client";

import { Icon } from "@/components/icons";
import { StatusBadge } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import type { ReportRoom, ReportSnapshot } from "@/lib/types";
import { ReportPhotoGallery } from "./ReportPhotoGallery";

type Summary = NonNullable<ReportSnapshot["comparison"]>;
type RoomComparison = NonNullable<ReportRoom["comparison"]>;

/** Move Out only: decision counts + one row per compared room. */
export function ComparisonSummary({ summary }: { summary: Summary }) {
  const t = useT(reportMessages);
  return (
    <section aria-labelledby="comparison-summary" className="border-b border-line px-5 py-6 sm:px-8">
      <div className="mb-4 flex flex-wrap items-baseline justify-between gap-2">
        <h2 id="comparison-summary" className="text-section font-semibold text-ink">{t("comparisonSummary")}</h2>
        <span className="text-label text-ink-3">{t("roomsCompared", { count: summary.roomsCompared })}</span>
      </div>
      <div className="mb-4 flex flex-wrap gap-2">
        {Object.entries(summary.decisionCounts).map(([decision, count]) => (
          <span key={decision} className="inline-flex items-center gap-1.5"><StatusBadge value={decision} /><span className="tabular text-label font-semibold text-ink">{count}</span></span>
        ))}
      </div>
      <div className="overflow-x-auto rounded-md border border-line">
        <table className="w-full text-left text-body">
          <thead className="bg-surface-2 text-caption font-medium uppercase tracking-wide text-ink-3">
            <tr><th className="px-3 py-2">{t("room")}</th><th className="px-3 py-2">{t("decision")}</th><th className="px-3 py-2">{t("notes")}</th></tr>
          </thead>
          <tbody className="divide-y divide-line">
            {summary.items.map((i) => (
              <tr key={i.roomName}>
                <td className="px-3 py-2 font-medium text-ink">{i.roomName}</td>
                <td className="px-3 py-2"><StatusBadge value={i.decision} /></td>
                <td className="px-3 py-2 text-ink-2">{i.notes ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

/**
 * Before / after for one room: the Move In record (text, defects, photos) beside the inspector's assessment.
 * The current condition is the room section itself, so it is not repeated here. Stacks on phones.
 */
export function RoomComparisonBlock({ comparison, photoUrls }: { comparison: RoomComparison; photoUrls: string[] }) {
  const t = useT(reportMessages);
  return (
    <div className="mt-5 overflow-hidden rounded-md border border-line">
      <div className="flex items-center gap-2 border-b border-line bg-surface-2 px-4 py-2 text-label font-semibold text-ink">
        <Icon name="compare" className="h-4 w-4 text-moveout" />{t("comparisonWithMoveIn")}
      </div>
      <div className="grid md:grid-cols-2 md:divide-x md:divide-line">
        <div className="space-y-3 p-4">
          <p className="text-caption font-medium uppercase tracking-wide text-movein">{t("moveInColumn")}</p>
          <p className="whitespace-pre-line text-body text-ink-2">{comparison.baselineDescription ?? "—"}</p>
          {comparison.baselineDefects.length > 0 && (
            <div>
              <p className="text-caption font-medium text-ink-3">{t("moveInDefects")}</p>
              <ul className="mt-1 list-disc space-y-0.5 pl-4 text-label text-ink-2">{comparison.baselineDefects.map((d) => <li key={d}>{d}</li>)}</ul>
            </div>
          )}
          {photoUrls.length > 0 ? <ReportPhotoGallery urls={photoUrls} size="sm" /> : <p className="text-label text-ink-4">{t("noBaselinePhotos")}</p>}
        </div>
        <div className="space-y-3 border-t border-line p-4 md:border-t-0">
          <p className="text-caption font-medium uppercase tracking-wide text-moveout">{t("differenceColumn")}</p>
          {comparison.decision && <StatusBadge value={comparison.decision} />}
          {comparison.notes && <p className="text-body text-ink-2"><span className="text-ink-3">{t("notesLabel")}</span> {comparison.notes}</p>}
          {(comparison.aiAnalysis ?? comparison.basicComparison) && (
            <div>
              <p className="text-caption font-medium text-ink-3">{t("possibleDifference")}</p>
              <p className="mt-0.5 whitespace-pre-line text-label text-ink-2">{comparison.aiAnalysis ?? comparison.basicComparison}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
