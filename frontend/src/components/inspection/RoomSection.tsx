"use client";

import type { ReactNode } from "react";
import { StatusBadge } from "@/components/ui";
import { useFormatters } from "@/i18n/I18nProvider";
import type { RoomDetail } from "@/lib/types";
import { roomSectionId } from "./RoomNavigation";

/**
 * The frame of one room on the execution pages. The <section id="room-…"> and the level-2 heading
 * "N. Room name" are anchors for the room navigation, deep links and the e2e flow — keep both.
 */
export function RoomSection({ room, aside, children }: { room: RoomDetail; aside?: ReactNode; children: ReactNode }) {
  const { humanize } = useFormatters();
  return (
    <section id={roomSectionId(room.id)} className="scroll-mt-64 rounded-lg border border-line bg-surface shadow-card lg:scroll-mt-44">
      <header className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b border-line px-4 py-3 sm:px-5">
        <h2 className="text-section font-semibold text-ink">{room.sequence}. {room.name}</h2>
        <span className="text-caption text-ink-3">{humanize(room.roomType)}</span>
        <span className="ml-auto flex items-center gap-2">{aside}<StatusBadge value={room.status} /></span>
      </header>
      <div className="space-y-5 p-4 sm:p-5">{children}</div>
    </section>
  );
}
