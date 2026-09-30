"use client";

import { useFormatters, useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import type { ReportPhoto, ReportSnapshot, ReportView as ReportViewData } from "@/lib/types";
import { PhotoStrip } from "./PhotoUploader";
import { Badge, Card, DefinitionList } from "./ui";

const urlsFor = (photos: ReportPhoto[], urls: Record<string, string>) => photos.map((p) => urls[p.mediaId]).filter(Boolean);

// Only the labels follow the viewer's language; the snapshot's content (room names, descriptions, notes,
// observations) is shown exactly as it was frozen.

/** Renders an immutable report snapshot (also used for the agent's pre-finalization preview). */
export function ReportBody({ snapshot, photoUrls, observations, renderRoomExtra }: {
  snapshot: ReportSnapshot; photoUrls: Record<string, string>;
  observations?: ReportViewData["observations"];
  renderRoomExtra?: (roomId: string) => React.ReactNode;
}) {
  const t = useT(reportMessages);
  const { formatDate, formatDateTime, humanize } = useFormatters();
  const s = snapshot;
  const address = [s.property.addressLine1, s.property.addressLine2, s.property.city, s.property.postcode, s.property.country].filter(Boolean).join(", ");
  const tenancyEnd = s.inspection.tenancyEndDate ? formatDate(s.inspection.tenancyEndDate) : t("ongoing");
  return (
    <div className="space-y-4">
      <Card>
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3 border-b border-slate-100 pb-4">
          <div>
            <div className="text-lg font-semibold text-brand">{s.company.name}</div>
            <div className="font-medium">{t("inspectionReportTitle", { type: humanize(s.inspection.type) })}</div>
            <div className="text-sm text-slate-600">{address}</div>
          </div>
          <div className="text-right text-sm text-slate-600">
            <div className="font-semibold text-slate-900">{s.reportNumber}</div>
            {s.versionNumber > 0 && <div>{t("version", { number: s.versionNumber })}</div>}
            <div>{t("generated", { date: formatDateTime(s.generatedAt) })}</div>
          </div>
        </div>
        <DefinitionList items={[
          [t("inspectionDate"), formatDate(s.inspection.completedAt)],
          [t("inspector"), s.agent?.name ?? "—"],
          [t("tenants"), s.tenants.map((tenant) => tenant.name).join(", ") || "—"],
          ...(s.inspection.tenancyStartDate ? [[t("tenancy"), t("tenancyRange", { start: formatDate(s.inspection.tenancyStartDate), end: tenancyEnd })] as [string, string]] : []),
          ...(s.inspection.comparisonReportNumber ? [[t("comparedWith"), t("comparedWithValue", { number: s.inspection.comparisonReportNumber })] as [string, string]] : []),
          [t("rooms"), String(s.rooms.length)],
        ]} />
        <p className="mt-4 rounded-lg bg-slate-50 p-3 text-xs italic text-slate-600">{t("disclaimer")}</p>
      </Card>

      {s.comparison && (
        <Card title={t("comparisonSummary")}>
          <div className="mb-3 flex flex-wrap gap-2">{Object.entries(s.comparison.decisionCounts).map(([k, v]) => <span key={k} className="flex items-center gap-1"><Badge value={k} /> <span className="text-sm">{v}</span></span>)}</div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead><tr className="border-b text-slate-500"><th className="py-2 pr-3">{t("room")}</th><th className="py-2 pr-3">{t("decision")}</th><th className="py-2">{t("notes")}</th></tr></thead>
              <tbody>{s.comparison.items.map((i) => <tr key={i.roomName} className="border-b border-slate-100"><td className="py-2 pr-3 font-medium">{i.roomName}</td><td className="py-2 pr-3"><Badge value={i.decision} /></td><td className="py-2 text-slate-700">{i.notes ?? "—"}</td></tr>)}</tbody>
            </table>
          </div>
        </Card>
      )}

      {s.rooms.map((r) => (
        <Card key={r.id} title={`${r.sequence}. ${r.name}`} actions={<span className="text-sm text-slate-500">{humanize(r.roomType)}</span>}>
          <p className="whitespace-pre-line text-sm text-slate-800">{r.description ?? t("noDescription")}</p>
          <div className="mt-3"><PhotoStrip urls={urlsFor(r.photos, photoUrls)} /></div>
          {r.defects.length > 0 ? (
            <div className="mt-4 space-y-3">
              <h4 className="text-sm font-semibold text-orange-800">{t("defectsCount", { count: r.defects.length })}</h4>
              {r.defects.map((d, i) => (
                <div key={d.id} className="border-l-2 border-amber-400 pl-3">
                  <div className="flex flex-wrap items-center gap-2 text-sm font-medium">{i + 1}. {d.title ?? t("defect")} {d.location && <span className="font-normal text-slate-500">· {d.location}</span>} <Badge value={d.classification} /></div>
                  <p className="text-sm text-slate-700">{d.description}</p>
                  {d.photos.length > 0 && <div className="mt-2"><PhotoStrip urls={urlsFor(d.photos, photoUrls)} /></div>}
                </div>
              ))}
            </div>
          ) : <p className="mt-3 text-sm text-slate-500">{t("noDefects")}</p>}
          {r.agentNotes && <p className="mt-3 text-sm"><span className="font-medium">{t("inspectorNotes")}</span> {r.agentNotes}</p>}
          {r.comparison && (
            <div className="mt-4 rounded-lg border border-violet-200 bg-violet-50/50 p-3 text-sm">
              <div className="mb-1 flex items-center gap-2 font-medium text-violet-900">{t("comparisonWithMoveIn")} {r.comparison.decision && <Badge value={r.comparison.decision} />}</div>
              <p className="text-slate-700"><span className="text-slate-500">{t("moveInRecord")}</span> {r.comparison.baselineDescription ?? "—"}</p>
              {r.comparison.notes && <p className="mt-1 text-slate-700"><span className="text-slate-500">{t("notesLabel")}</span> {r.comparison.notes}</p>}
              {r.comparison.baselinePhotos.length > 0 && <details className="mt-2"><summary className="cursor-pointer text-slate-600">{t("moveInPhotos")}</summary><div className="mt-2"><PhotoStrip urls={urlsFor(r.comparison.baselinePhotos, photoUrls)} /></div></details>}
            </div>
          )}
          {observations?.filter((o) => o.roomId === r.id).map((o) => (
            <div key={o.id} className="mt-3 rounded-lg bg-sky-50 p-3 text-sm"><span className="font-medium">{t("tenantObservation", { name: o.authorName })}</span> {o.text}</div>
          ))}
          {renderRoomExtra?.(r.id)}
        </Card>
      ))}
    </div>
  );
}

export function ReportMeta({ report }: { report: ReportViewData }) {
  const t = useT(reportMessages);
  const { formatDateTime } = useFormatters();
  const general = report.observations.filter((o) => !o.roomId);
  return (
    <div className="space-y-4">
      {(general.length > 0 || report.responses.length > 0) && (
        <Card title={t("tenantReview")}>
          {report.responses.map((r) => (
            <div key={r.id} className="mb-2 text-sm"><Badge value={r.decision} /> <span className="font-medium">{r.userName}</span> · {formatDateTime(r.createdAt)}{r.comment && <p className="mt-1 text-slate-700">{r.comment}</p>}</div>
          ))}
          {general.map((o) => <p key={o.id} className="mt-2 rounded-lg bg-sky-50 p-3 text-sm"><span className="font-medium">{o.authorName}:</span> {o.text}</p>)}
        </Card>
      )}
      <Card title={t("integrity")}>
        <DefinitionList items={[[t("report"), t("reportVersion", { number: report.reportNumber, version: report.versionNumber })], [t("frozenAt"), formatDateTime(report.generatedAt)],
          [t("snapshotSha"), <code key="h" className="break-all text-xs">{report.snapshotSha256}</code>]]} />
        <p className="mt-2 text-xs text-slate-500">{t("immutableNote")}</p>
      </Card>
    </div>
  );
}
