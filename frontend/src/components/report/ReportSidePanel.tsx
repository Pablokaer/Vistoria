"use client";

import { Icon } from "@/components/icons";
import { StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import type { ReportSnapshot, ReportView } from "@/lib/types";
import { reportRoomAnchor } from "./ReportRoomSection";

const PANEL = "rounded-lg border border-line bg-surface p-4 shadow-card";
const PANEL_TITLE = "mb-3 text-caption font-semibold uppercase tracking-wide text-ink-3";

/** Room index with defect markers; jumps to the room inside the document. Hidden on phones (the page scrolls). */
export function ReportContents({ snapshot }: { snapshot: ReportSnapshot }) {
  const t = useT(reportMessages);
  return (
    <nav aria-label={t("contents")} className={`${PANEL} hidden xl:block`}>
      <p className={PANEL_TITLE}>{t("contents")}</p>
      <ol className="space-y-0.5">
        {snapshot.rooms.map((r) => (
          <li key={r.id}>
            <a href={`#${reportRoomAnchor(r.id)}`} className="flex items-center gap-2 rounded-md px-2 py-1.5 text-label text-ink-2 hover:bg-surface-2 hover:text-ink">
              <span className="tabular w-5 text-ink-4">{r.sequence}</span>
              <span className="min-w-0 flex-1 truncate">{r.name}</span>
              {r.defects.length > 0 && <Icon name="alert" className="h-3.5 w-3.5 text-warning-500" aria-label={t("defectsBadge", { count: r.defects.length })} />}
            </a>
          </li>
        ))}
      </ol>
    </nav>
  );
}

function TenantReview({ report }: { report: ReportView }) {
  const t = useT(reportMessages);
  const { formatDateTime } = useFormatters();
  const general = report.observations.filter((o) => !o.roomId);
  if (general.length === 0 && report.responses.length === 0) return null;
  return (
    <section className={PANEL}>
      <p className={PANEL_TITLE}>{t("tenantReview")}</p>
      <ul className="space-y-3">
        {report.responses.map((r) => (
          <li key={r.id} className="text-label">
            <div className="flex flex-wrap items-center gap-2"><StatusBadge value={r.decision} /><span className="font-medium text-ink">{r.userName}</span></div>
            <p className="mt-0.5 text-caption text-ink-3">{formatDateTime(r.createdAt)}</p>
            {r.comment && <p className="mt-1 text-ink-2">{r.comment}</p>}
          </li>
        ))}
      </ul>
      {general.length > 0 && (
        <div className={report.responses.length > 0 ? "mt-4 border-t border-line pt-3" : ""}>
          <p className="mb-2 text-caption font-medium text-ink-3">{t("generalObservations")}</p>
          <ul className="space-y-2">{general.map((o) => <li key={o.id} className="rounded-md bg-info-50 p-2.5 text-label text-ink-2"><span className="font-medium text-info-700">{o.authorName}:</span> {o.text}</li>)}</ul>
        </div>
      )}
    </section>
  );
}

function Versions({ report }: { report: ReportView }) {
  const t = useT(reportMessages);
  const { formatDateTime } = useFormatters();
  if (report.versions.length === 0) return null;
  return (
    <section className={PANEL}>
      <p className={PANEL_TITLE}>{t("versions")}</p>
      <ol className="space-y-2">
        {report.versions.map((v) => (
          <li key={v.versionNumber} className="flex items-start gap-2 text-label">
            <Icon name={v.versionNumber === report.versionNumber ? "checkCircle" : "history"} className={v.versionNumber === report.versionNumber ? "mt-0.5 h-4 w-4 text-success-500" : "mt-0.5 h-4 w-4 text-ink-4"} />
            <div className="min-w-0"><p className="font-medium text-ink">{t("versionLine", { number: v.versionNumber })}</p><p className="text-caption text-ink-3">{formatDateTime(v.generatedAt)}</p></div>
          </li>
        ))}
      </ol>
    </section>
  );
}

function Integrity({ report }: { report: ReportView }) {
  const t = useT(reportMessages);
  const { formatDateTime } = useFormatters();
  return (
    <section className={PANEL}>
      <p className={`${PANEL_TITLE} flex items-center gap-1.5`}><Icon name="shield" className="h-4 w-4" />{t("integrity")}</p>
      <dl className="space-y-2 text-label">
        <div><dt className="text-caption text-ink-3">{t("report")}</dt><dd className="font-medium text-ink">{t("reportVersion", { number: report.reportNumber, version: report.versionNumber })}</dd></div>
        <div><dt className="text-caption text-ink-3">{t("frozenAt")}</dt><dd className="font-medium text-ink">{formatDateTime(report.generatedAt)}</dd></div>
        <div><dt className="text-caption text-ink-3">{t("snapshotSha")}</dt><dd><code className="block break-all font-mono text-[11px] leading-snug text-ink-2">{report.snapshotSha256}</code></dd></div>
      </dl>
      <p className="mt-3 text-caption leading-relaxed text-ink-3">{t("immutableNote")}</p>
    </section>
  );
}

/** Everything about the report that is not the document itself: contents, tenant review, versions, integrity. */
export function ReportMeta({ report, showContents = true }: { report: ReportView; showContents?: boolean }) {
  return (
    <div className="space-y-4">
      {showContents && <ReportContents snapshot={report.snapshot} />}
      <TenantReview report={report} />
      <Versions report={report} />
      <Integrity report={report} />
    </div>
  );
}
