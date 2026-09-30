"use client";

import type { ReactNode } from "react";
import { IconButton } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { scrollToRoom, type RoomNavItem } from "./RoomNavigation";

/** Previous / next room with "Room 2 of 6 · Kitchen" — lets the inspector walk the property room by room. */
export function RoomStepper({ items, currentId }: { items: RoomNavItem[]; currentId: string | null }) {
  const t = useT(captureMessages);
  const index = Math.max(0, items.findIndex((r) => r.id === currentId));
  const current = items[index];
  if (!current) return null;
  const go = (i: number) => { const target = items[i]; if (target) scrollToRoom(target.id); };
  return (
    <div className="flex min-w-0 items-center gap-1">
      <IconButton icon="chevronLeft" label={t("previousRoom")} disabled={index === 0} onClick={() => go(index - 1)} className="h-10 w-10 disabled:opacity-40" />
      <div className="min-w-0 px-1 leading-tight">
        <p className="tabular text-caption text-ink-3">{t("roomOf", { current: index + 1, total: items.length })}</p>
        <p className="truncate text-label font-semibold text-ink">{current.name}</p>
      </div>
      <IconButton icon="chevronRight" label={t("nextRoom")} disabled={index === items.length - 1} onClick={() => go(index + 1)} className="h-10 w-10 disabled:opacity-40" />
    </div>
  );
}

/**
 * Fixed action bar for the step's primary action. On phones it sits above the app's bottom tab bar and shows the
 * room stepper; on desktop it spans the content area next to the sidebar.
 */
export function ExecutionBar({ stepper, hint, children }: { stepper?: ReactNode; hint?: ReactNode; children: ReactNode }) {
  return (
    <div className="fixed inset-x-0 bottom-[calc(3.5rem+env(safe-area-inset-bottom))] z-30 border-t border-line bg-surface/95 shadow-overlay backdrop-blur lg:bottom-0 lg:left-sidebar lg:shadow-none">
      <div className="mx-auto flex max-w-page flex-wrap items-center gap-x-4 gap-y-2 px-4 py-2.5 sm:px-6 lg:px-8 lg:py-3">
        {stepper && <div className="min-w-0 flex-1 lg:flex-none">{stepper}</div>}
        {hint && <p className="hidden flex-1 text-label text-ink-3 lg:block">{hint}</p>}
        <div className="w-full sm:ml-auto sm:w-auto [&>*]:w-full sm:[&>*]:w-auto">{children}</div>
      </div>
    </div>
  );
}
