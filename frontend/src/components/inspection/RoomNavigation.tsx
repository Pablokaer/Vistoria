"use client";

import { useEffect, useRef, useState, type MouseEvent } from "react";
import { Icon, type IconName } from "@/components/icons";
import { Spinner } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { cx } from "@/lib/cx";

export type RoomNavState = "done" | "ready" | "pending" | "writing" | "attention";
export interface RoomNavItem { id: string; name: string; sequence: number; state: RoomNavState }

const STATE: Record<RoomNavState, { icon: IconName; className: string; label: "stateDone" | "stateReady" | "statePending" | "stateWriting" | "stateAttention" }> = {
  done: { icon: "checkCircle", className: "text-success-500", label: "stateDone" },
  ready: { icon: "checkCircle", className: "text-success-500", label: "stateReady" },
  pending: { icon: "circleDashed", className: "text-ink-4", label: "statePending" },
  writing: { icon: "sparkles", className: "text-review-500", label: "stateWriting" },
  attention: { icon: "alert", className: "text-warning-500", label: "stateAttention" },
};

/** Section ids are `room-<id>` (the e2e and the "Edit photos" deep links rely on them). */
export const roomSectionId = (roomId: string) => `room-${roomId}`;

/** Smoothly scrolls a room section into view (its scroll-margin leaves room for the sticky header). */
export function scrollToRoom(roomId: string) {
  document.getElementById(roomSectionId(roomId))?.scrollIntoView({ behavior: "smooth", block: "start" });
}

/** The room whose section crosses the upper-middle of the viewport. Example: `const current = useCurrentRoom(ids)`. */
export function useCurrentRoom(roomIds: string[]): string | null {
  const [current, setCurrent] = useState<string | null>(roomIds[0] ?? null);
  const key = roomIds.join(",");
  useEffect(() => {
    const ids = key ? key.split(",") : [];
    const observer = new IntersectionObserver((entries) => {
      const visible = entries.filter((e) => e.isIntersecting).map((e) => e.target.id.replace(/^room-/, ""));
      if (visible.length > 0) setCurrent(visible[0]);
    }, { rootMargin: "-35% 0px -55% 0px" });
    ids.forEach((id) => { const el = document.getElementById(roomSectionId(id)); if (el) observer.observe(el); });
    return () => observer.disconnect();
  }, [key]);
  return current;
}

function StateIcon({ state }: { state: RoomNavState }) {
  const t = useT(captureMessages);
  const s = STATE[state];
  return (
    <span className="shrink-0">
      {state === "writing" ? <span className={s.className}><Spinner small /></span> : <Icon name={s.icon} className={cx("h-4 w-4", s.className)} />}
      <span className="sr-only">{t(s.label)}</span>
    </span>
  );
}

function jump(e: MouseEvent, roomId: string) {
  e.preventDefault();
  scrollToRoom(roomId);
}

/** Desktop: sticky vertical list of rooms with their state. */
export function RoomNavList({ items, currentId }: { items: RoomNavItem[]; currentId: string | null }) {
  const t = useT(captureMessages);
  return (
    <nav aria-label={t("roomsNav")} className="sticky top-44 hidden max-h-[calc(100vh-12rem)] overflow-y-auto lg:block">
      <p className="mb-2 px-2 text-caption font-semibold uppercase tracking-wide text-ink-3">{t("roomsNav")}</p>
      <ul className="space-y-0.5">
        {items.map((r) => {
          const active = r.id === currentId;
          return (
            <li key={r.id}>
              <a href={`#${roomSectionId(r.id)}`} onClick={(e) => jump(e, r.id)} aria-current={active ? "location" : undefined}
                className={cx("flex items-center gap-2.5 rounded-md px-2 py-2 text-label transition",
                  active ? "bg-surface font-semibold text-ink shadow-card ring-1 ring-line" : "text-ink-2 hover:bg-surface/70")}>
                <span className="tabular w-4 text-caption text-ink-4">{r.sequence}</span>
                <span className="min-w-0 flex-1 truncate">{r.name}</span>
                <StateIcon state={r.state} />
              </a>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

/** Phones/tablets: horizontal chip strip; the current room scrolls into view as you move down the page. */
export function RoomChipStrip({ items, currentId }: { items: RoomNavItem[]; currentId: string | null }) {
  const t = useT(captureMessages);
  const refs = useRef(new Map<string, HTMLAnchorElement>());
  useEffect(() => {
    if (currentId) refs.current.get(currentId)?.scrollIntoView({ block: "nearest", inline: "center", behavior: "smooth" });
  }, [currentId]);
  return (
    <nav aria-label={t("roomsNav")} className="relative -mx-4 overflow-x-auto px-4 [scrollbar-width:none] sm:-mx-6 sm:px-6">
      <ul className="flex gap-1.5">
        {items.map((r) => {
          const active = r.id === currentId;
          return (
            <li key={r.id} className="shrink-0">
              <a ref={(el) => { if (el) refs.current.set(r.id, el); else refs.current.delete(r.id); }}
                href={`#${roomSectionId(r.id)}`} onClick={(e) => jump(e, r.id)} aria-current={active ? "location" : undefined}
                className={cx("flex min-h-9 items-center gap-1.5 rounded-full border px-3 text-label font-medium transition",
                  active ? "border-brand bg-brand-50 text-brand" : "border-line bg-surface text-ink-2")}>
                <StateIcon state={r.state} />{r.name}
              </a>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
