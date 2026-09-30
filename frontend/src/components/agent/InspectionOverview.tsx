"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { Icon } from "@/components/icons";
import { Card, DefinitionList, ProgressBar, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import type { InspectionDetails, RoomProgress } from "@/lib/types";

function RoomFacts({ room }: { room: RoomProgress }) {
  const t = useT(agentMessages);
  return (
    <span className="flex flex-wrap items-center gap-x-3 gap-y-0.5 text-caption text-ink-3">
      <span className="inline-flex items-center gap-1"><Icon name="camera" className="h-3.5 w-3.5 text-ink-4" />{t("roomPhotoCount", { count: room.generalPhotoCount })}</span>
      {room.defectCount > 0 && <span className="inline-flex items-center gap-1 text-warning-700"><Icon name="alert" className="h-3.5 w-3.5" />{t("roomDefectCount", { count: room.defectCount })}</span>}
      {room.hasFinalDescription && <span className="inline-flex items-center gap-1"><Icon name="file" className="h-3.5 w-3.5 text-ink-4" />{t("roomDescribed")}</span>}
      {room.comparisonDecided === false && <span className="inline-flex items-center gap-1 text-review-700"><Icon name="compare" className="h-3.5 w-3.5" />{t("roomComparisonPending")}</span>}
    </span>
  );
}

/** Rooms in sequence with their status; while editable each row jumps to that room on the photos page. */
export function RoomProgressList({ inspection, editable }: { inspection: InspectionDetails; editable: boolean }) {
  const t = useT(agentMessages);
  const rooms = [...inspection.rooms].sort((a, b) => a.sequence - b.sequence);
  return (
    <Card flush title={t("rooms")} description={t("roomsSubtitle", { done: inspection.roomsCompleted, total: rooms.length })}>
      <div className="px-4 pt-3 sm:px-5"><ProgressBar value={inspection.roomsCompleted} total={rooms.length} compact /></div>
      <ul className="mt-2 divide-y divide-line">
        {rooms.map((r) => {
          const row = (
            <div className="flex min-h-14 items-center gap-3 px-4 py-3 sm:px-5">
              <span className="tabular grid h-7 w-7 shrink-0 place-items-center rounded-md bg-neutral-50 text-caption font-semibold text-ink-2">{r.sequence}</span>
              <div className="min-w-0 flex-1">
                <p className="truncate text-body font-medium text-ink">{r.name}</p>
                <RoomFacts room={r} />
              </div>
              <StatusBadge value={r.status} />
              {editable && <Icon name="chevronRight" className="h-4 w-4 shrink-0 text-ink-4" />}
            </div>
          );
          return (
            <li key={r.id}>
              {editable ? <Link href={`/agent/inspections/${inspection.id}/capture#room-${r.id}`} aria-label={t("openRoom", { room: r.name })} className="block transition hover:bg-surface-2">{row}</Link> : row}
            </li>
          );
        })}
      </ul>
    </Card>
  );
}

function FactGroup({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="px-4 py-4 sm:px-5">
      <h3 className="mb-3 text-label font-semibold text-ink">{title}</h3>
      {children}
    </div>
  );
}

/** Everything about the inspection that is reference, grouped by meaning in one panel. */
export function InspectionFacts({ inspection: i }: { inspection: InspectionDetails }) {
  const t = useT(agentMessages);
  const { formatDate, formatDateTime, humanize } = useFormatters();
  const tenants = i.tenancy?.members.map((m) => m.fullName).join(", ") || "—";
  const dates: [string, string][] = [[t("scheduled"), i.scheduledDate ? formatDate(i.scheduledDate) : t("notScheduled")]];
  if (i.acceptedAt) dates.push([t("accepted"), formatDateTime(i.acceptedAt)]);
  if (i.startedAt) dates.push([t("started"), formatDateTime(i.startedAt)]);
  if (i.completedAt) dates.push([t("completedAt"), formatDateTime(i.completedAt)]);
  return (
    <section className="divide-y divide-line rounded-lg border border-line bg-surface shadow-card">
      <FactGroup title={t("propertyAndTenancy")}>
        <DefinitionList items={[[t("company"), i.companyName], [t("property"), humanize(i.property.propertyType)], [t("tenants"), tenants],
          ...(i.tenancy?.reference ? [[t("tenancyReference"), i.tenancy.reference] as [string, string]] : [])]} />
      </FactGroup>
      <FactGroup title={t("schedule")}><DefinitionList items={dates} /></FactGroup>
      {i.comparisonInspection && (
        <FactGroup title={t("comparisonTitle")}>
          <p className="flex gap-2 text-body text-ink-2"><Icon name="compare" className="mt-0.5 h-4 w-4 shrink-0 text-review-500" />
            {t("comparisonBody", { number: i.comparisonInspection.reportNumber ?? "—", date: formatDate(i.comparisonInspection.completedAt) })}</p>
        </FactGroup>
      )}
      {i.instructions && <FactGroup title={t("instructions")}><p className="whitespace-pre-line text-body text-ink-2">{i.instructions}</p></FactGroup>}
    </section>
  );
}
