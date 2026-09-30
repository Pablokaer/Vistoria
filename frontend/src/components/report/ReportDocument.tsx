"use client";

import type { ReactNode } from "react";
import { Icon } from "@/components/icons";
import { InspectionTypeTag } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import type { Formatters } from "@/i18n/formatting";
import { reportMessages, type ReportKey } from "@/i18n/messages/report";
import type { Translate } from "@/i18n/translate";
import type { ReportSnapshot, ReportView } from "@/lib/types";
import { ComparisonSummary } from "./ReportComparison";
import { ReportRoomSection } from "./ReportRoomSection";

type Observation = ReportView["observations"][number];

// Only the labels follow the viewer's language; the snapshot's content (room names, descriptions, notes,
// observations) is shown exactly as it was frozen.

const fullAddress = (p: ReportSnapshot["property"]) =>
  [p.addressLine1, p.addressLine2, p.city, p.postcode, p.country].filter(Boolean).join(", ");

// `embedded`: the document sits inside another page that already owns the h1 (agent review preview).
function DocumentHeader({ snapshot: s, embedded }: { snapshot: ReportSnapshot; embedded?: boolean }) {
  const Title = embedded ? "h2" : "h1";
  const t = useT(reportMessages);
  const { formatDateTime, humanize } = useFormatters();
  return (
    <header className="border-b border-line px-5 pb-5 pt-6 sm:px-8">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="text-label font-semibold uppercase tracking-wider text-brand">{s.company.name}</p>
          <Title className="mt-1 text-page font-semibold tracking-tight text-ink">{t("inspectionReportTitle", { type: humanize(s.inspection.type) })}</Title>
          <p className="mt-1 flex items-start gap-1.5 text-body text-ink-3"><Icon name="mapPin" className="mt-0.5 h-4 w-4 shrink-0" />{fullAddress(s.property)}</p>
        </div>
        <div className="shrink-0 rounded-md border border-line bg-surface-2 px-3 py-2 text-right">
          <p className="text-caption font-medium uppercase tracking-wide text-ink-3">{t("reportNumber")}</p>
          <p className="tabular text-card-title font-semibold text-ink">{s.reportNumber}</p>
          {s.versionNumber > 0 && <p className="text-caption text-ink-3">{t("version", { number: s.versionNumber })}</p>}
        </div>
      </div>
      <p className="mt-3 text-caption text-ink-3">{t("generated", { date: formatDateTime(s.generatedAt) })}</p>
    </header>
  );
}

function metadataItems(s: ReportSnapshot, t: Translate<ReportKey>, f: Formatters): [string, ReactNode][] {
  const items: [string, ReactNode][] = [
    [t("inspectionDate"), f.formatDate(s.inspection.completedAt)],
    [t("inspector"), s.agent?.name ?? "—"],
    [t("tenants"), s.tenants.map((tenant) => tenant.name).join(", ") || "—"],
    [t("propertyType"), f.humanize(s.property.propertyType)],
    [t("rooms"), String(s.rooms.length)],
  ];
  if (s.inspection.tenancyStartDate) {
    const end = s.inspection.tenancyEndDate ? f.formatDate(s.inspection.tenancyEndDate) : t("ongoing");
    items.push([t("tenancy"), t("tenancyRange", { start: f.formatDate(s.inspection.tenancyStartDate), end })]);
  }
  if (s.inspection.comparisonReportNumber) items.push([t("comparedWith"), t("comparedWithValue", { number: s.inspection.comparisonReportNumber })]);
  return items;
}

function DocumentMetadata({ snapshot: s }: { snapshot: ReportSnapshot }) {
  const t = useT(reportMessages);
  const f = useFormatters();
  return (
    <div className="border-b border-line px-5 py-5 sm:px-8">
      <div className="mb-4"><InspectionTypeTag type={s.inspection.type} /></div>
      <dl className="grid grid-cols-2 gap-x-6 gap-y-3 sm:grid-cols-3">
        {metadataItems(s, t, f).map(([label, value]) => (
          <div key={label} className="min-w-0">
            <dt className="text-caption font-medium uppercase tracking-wide text-ink-3">{label}</dt>
            <dd className="mt-0.5 text-body font-medium text-ink">{value}</dd>
          </div>
        ))}
      </dl>
      <p className="mt-5 flex gap-2 rounded-md bg-surface-2 p-3 text-caption italic leading-relaxed text-ink-3"><Icon name="shield" className="h-4 w-4 shrink-0 not-italic" />{t("disclaimer")}</p>
    </div>
  );
}

/**
 * Renders an immutable report snapshot as a document (also the agent's pre-finalization preview).
 * `renderRoomExtra` adds page-specific controls at the end of each room (tenant observation, agent edit).
 * Example: `<ReportBody snapshot={s} photoUrls={urls} observations={obs} />`
 */
export function ReportBody({ snapshot, photoUrls, observations, renderRoomExtra, embedded }: {
  snapshot: ReportSnapshot; photoUrls: Record<string, string>; embedded?: boolean;
  observations?: Observation[];
  renderRoomExtra?: (roomId: string) => ReactNode;
}) {
  const t = useT(reportMessages);
  return (
    <article aria-label={t("documentLabel")} className="overflow-hidden rounded-lg border border-line bg-surface shadow-card">
      <DocumentHeader snapshot={snapshot} embedded={embedded} />
      <DocumentMetadata snapshot={snapshot} />
      {snapshot.comparison && <ComparisonSummary summary={snapshot.comparison} />}
      {snapshot.rooms.map((room) => (
        <ReportRoomSection key={room.id} room={room} photoUrls={photoUrls}
          observations={observations?.filter((o) => o.roomId === room.id) ?? []} extra={renderRoomExtra?.(room.id)} />
      ))}
    </article>
  );
}
