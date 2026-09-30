"use client";

import type { ReactNode } from "react";
import { Icon } from "@/components/icons";
import { StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import type { ReportDefect, ReportPhoto, ReportRoom, ReportView } from "@/lib/types";
import { RoomComparisonBlock } from "./ReportComparison";
import { ReportPhotoGallery } from "./ReportPhotoGallery";

type Observation = ReportView["observations"][number];

export const urlsFor = (photos: ReportPhoto[], urls: Record<string, string>): string[] => photos.map((p) => urls[p.mediaId]).filter(Boolean);

/** Anchor id of a room section (used by the side-panel contents). */
export const reportRoomAnchor = (roomId: string) => `report-room-${roomId}`;

function DefectItem({ defect, index, photoUrls }: { defect: ReportDefect; index: number; photoUrls: Record<string, string> }) {
  const t = useT(reportMessages);
  return (
    <li className="rounded-md border border-line bg-surface-2/60 p-3">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-body font-semibold text-ink">{index}. {defect.title ?? t("defect")}</span>
        {defect.location && <span className="inline-flex items-center gap-1 text-label text-ink-3"><Icon name="mapPin" className="h-3.5 w-3.5" />{defect.location}</span>}
        <StatusBadge value={defect.classification} className="ml-auto" />
      </div>
      {defect.description && <p className="mt-1.5 text-body text-ink-2">{defect.description}</p>}
      {defect.photos.length > 0 && <div className="mt-2"><ReportPhotoGallery urls={urlsFor(defect.photos, photoUrls)} size="sm" /></div>}
    </li>
  );
}

function RoomHeader({ room }: { room: ReportRoom }) {
  const t = useT(reportMessages);
  const { humanize } = useFormatters();
  return (
    <div className="mb-3 flex flex-wrap items-baseline gap-x-3 gap-y-1">
      <h2 className="text-section font-semibold text-ink">{room.sequence}. {room.name}</h2>
      <span className="text-label text-ink-3">{humanize(room.roomType)}</span>
      <span className="ml-auto inline-flex items-center gap-3 text-caption text-ink-3">
        <span className="inline-flex items-center gap-1"><Icon name="image" className="h-3.5 w-3.5" />{room.photos.length === 1 ? t("photosCountOne") : t("photosCount", { count: room.photos.length })}</span>
        <span className={room.defects.length > 0 ? "inline-flex items-center gap-1 font-medium text-warning-700" : "inline-flex items-center gap-1"}>
          <Icon name={room.defects.length > 0 ? "alert" : "check"} className="h-3.5 w-3.5" />
          {room.defects.length > 0 ? (room.defects.length === 1 ? t("defectsBadgeOne") : t("defectsBadge", { count: room.defects.length })) : t("noDefectsBadge")}
        </span>
      </span>
    </div>
  );
}

/**
 * One room of the report document: condition text, photos, defects, inspector notes, Move In comparison,
 * tenant observations and an optional slot for page-specific actions (tenant observation, agent edit).
 */
export function ReportRoomSection({ room, photoUrls, observations, extra }:
  { room: ReportRoom; photoUrls: Record<string, string>; observations: Observation[]; extra?: ReactNode }) {
  const t = useT(reportMessages);
  return (
    <section id={reportRoomAnchor(room.id)} className="scroll-mt-24 border-b border-line px-5 py-6 last:border-b-0 sm:px-8">
      <RoomHeader room={room} />
      <p className="whitespace-pre-line text-body leading-relaxed text-ink-2">{room.description ?? t("noDescription")}</p>
      {room.photos.length > 0 && <div className="mt-4"><ReportPhotoGallery urls={urlsFor(room.photos, photoUrls)} /></div>}
      {room.defects.length > 0 && (
        <div className="mt-5">
          <h3 className="mb-2 text-label font-semibold text-warning-700">{t("defectsCount", { count: room.defects.length })}</h3>
          <ol className="space-y-2">{room.defects.map((d, i) => <DefectItem key={d.id} defect={d} index={i + 1} photoUrls={photoUrls} />)}</ol>
        </div>
      )}
      {room.agentNotes && <p className="mt-4 text-body text-ink-2"><span className="font-medium text-ink">{t("inspectorNotes")}</span> {room.agentNotes}</p>}
      {room.comparison && <RoomComparisonBlock comparison={room.comparison} photoUrls={urlsFor(room.comparison.baselinePhotos, photoUrls)} />}
      {observations.map((o) => (
        <div key={o.id} className="mt-4 flex gap-3 rounded-md border border-info-500/25 bg-info-50 p-3 text-body text-info-700">
          <Icon name="message" className="mt-0.5 h-4 w-4 shrink-0" />
          <p><span className="font-semibold">{t("tenantObservation", { name: o.authorName })}</span> <span className="text-ink-2">{o.text}</span></p>
        </div>
      ))}
      {extra}
    </section>
  );
}
