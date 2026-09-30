"use client";

import type { ReactNode } from "react";
import { Icon } from "@/components/icons";
import { RoomNavList, type RoomNavItem } from "./RoomNavigation";

/** Rooms on the left (desktop only), room sections on the right. Bottom padding clears the fixed action bar. */
export function ExecutionLayout({ nav, currentId, children }: { nav: RoomNavItem[]; currentId: string | null; children: ReactNode }) {
  return (
    <div className="grid gap-6 pb-44 lg:grid-cols-[13rem_minmax(0,1fr)] lg:pb-24">
      <aside className="hidden lg:block"><RoomNavList items={nav} currentId={currentId} /></aside>
      <div className="min-w-0 space-y-5">{children}</div>
    </div>
  );
}

/** List of problems that block moving on, shown above the rooms. */
export function IssueList({ title, issues }: { title?: string; issues: string[] }) {
  if (issues.length === 0) return null;
  return (
    <div role="alert" className="flex gap-3 rounded-lg border border-danger-500/30 bg-danger-50 px-4 py-3 text-body text-danger-700 animate-fade-in">
      <Icon name="alert" className="mt-0.5 h-5 w-5 shrink-0" />
      <div className="min-w-0">
        {title && <p className="font-semibold">{title}</p>}
        <ul className="mt-1 list-disc space-y-0.5 pl-5">{issues.map((i) => <li key={i}>{i}</li>)}</ul>
      </div>
    </div>
  );
}
