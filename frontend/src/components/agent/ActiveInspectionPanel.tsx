"use client";

import Link from "next/link";
import { Icon } from "@/components/icons";
import { InspectionTypeTag, LinkButton, ProgressBar, PropertyThumb, Skeleton, StatusBadge, VisibilityTag } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentDashboardMessages } from "@/i18n/messages/agent-dashboard";
import { cx } from "@/lib/cx";
import { useApi } from "@/lib/hooks";
import type { InspectionDetails, InspectionSummary, RoomProgress } from "@/lib/types";
import { continueHref } from "./activeInspection";

/** Where the inspector is in the property: one chip per room, done rooms ticked. */
function RoomTrack({ rooms }: { rooms: RoomProgress[] }) {
  const d = useT(agentDashboardMessages);
  const sorted = [...rooms].sort((a, b) => a.sequence - b.sequence);
  return (
    <ul className="flex flex-wrap gap-1.5">
      {sorted.map((r) => {
        const done = r.status === "Completed";
        return (
          <li key={r.id} title={d(done ? "roomDone" : "roomOpen", { room: r.name })}
            className={cx("inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-caption font-medium ring-1 ring-inset",
              done ? "bg-success-50 text-success-700 ring-success-700/15" : r.status === "InProgress" ? "bg-brand-50 text-brand ring-brand/20" : "bg-surface text-ink-3 ring-line")}>
            <Icon name={done ? "check" : r.status === "InProgress" ? "play" : "circleDashed"} className="h-3 w-3" />
            {r.name}
          </li>
        );
      })}
    </ul>
  );
}

/**
 * The inspection to continue now, given the most visual weight on the dashboard: property, progress per room,
 * last update and a single primary action that lands on the right working screen.
 */
export function ActiveInspectionPanel({ item }: { item: InspectionSummary }) {
  const d = useT(agentDashboardMessages);
  const { formatDate, formatDateTime } = useFormatters();
  const { data: details } = useApi<InspectionDetails>(`/api/agent/inspections/${item.id}`);
  const label = item.status === "Review" ? d("openReview") : item.status === "Assigned" ? d("openAssigned") : d("continueCapture");
  return (
    <section aria-labelledby="current-inspection" className="overflow-hidden rounded-lg border border-brand/25 bg-surface shadow-raised">
      <div className="flex items-center justify-between gap-3 border-b border-line bg-brand-50/60 px-4 py-2.5 sm:px-5">
        <p id="current-inspection" className="text-caption font-semibold uppercase tracking-wide text-brand">
          {item.status === "Assigned" ? d("nextUp") : d("currentInspection")}
        </p>
        <StatusBadge value={item.status} />
      </div>
      <div className="p-4 sm:p-5">
        <div className="flex items-start gap-4">
          <PropertyThumb type={item.inspectionType} size="lg" />
          <div className="min-w-0 flex-1">
            <div className="mb-1 flex flex-wrap items-center gap-2"><InspectionTypeTag type={item.inspectionType} /><VisibilityTag visibility={item.visibility} /></div>
            <Link href={`/agent/inspections/${item.id}`} className="block text-section font-semibold text-ink hover:text-brand sm:text-[1.125rem]">{item.propertyAddress}</Link>
            <p className="mt-1 flex flex-wrap gap-x-4 gap-y-1 text-label text-ink-3">
              {item.scheduledDate && <span className="inline-flex items-center gap-1.5"><Icon name="calendar" className="h-4 w-4 text-ink-4" />{d("scheduledFor", { date: formatDate(item.scheduledDate) })}</span>}
              <span className="inline-flex items-center gap-1.5"><Icon name="clock" className="h-4 w-4 text-ink-4" />{d("lastUpdate", { date: formatDateTime(item.updatedAt) })}</span>
            </p>
          </div>
        </div>
        <div className="mt-4">
          <ProgressBar value={item.roomsCompleted} total={item.roomsTotal} label={d("roomsProgress", { done: item.roomsCompleted, total: item.roomsTotal })} />
        </div>
        <div className="mt-3 min-h-6">{details ? <RoomTrack rooms={details.rooms} /> : <Skeleton className="h-6 w-2/3" />}</div>
        <div className="mt-5 flex flex-col gap-2 sm:flex-row sm:items-center">
          <LinkButton href={continueHref(item)} size="lg" icon={item.status === "Review" ? "eye" : "play"} className="w-full sm:w-auto">{label}</LinkButton>
        </div>
      </div>
    </section>
  );
}
