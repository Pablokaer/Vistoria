// Which of the inspector's active inspections deserves the spotlight, and where "continue" should land.
import type { InspectionStatus, InspectionSummary } from "@/lib/types";

// Work already started beats work waiting for review, which beats work not started yet.
const PRIORITY: Partial<Record<InspectionStatus, number>> = { InProgress: 0, Review: 1, Assigned: 2 };

/**
 * The inspection to continue first: highest-priority status, then the most recently touched.
 * Example: `pickCurrent(dashboard.active)?.id`.
 */
export function pickCurrent(active: InspectionSummary[]): InspectionSummary | null {
  const ranked = active.filter((i) => PRIORITY[i.status] !== undefined).sort((a, b) =>
    (PRIORITY[a.status]! - PRIORITY[b.status]!) || b.updatedAt.localeCompare(a.updatedAt));
  return ranked[0] ?? null;
}

/**
 * One click to the next working screen: photos while rooms are open, descriptions once every room has been
 * visited, the review page in Review; Assigned goes to the overview because starting needs an explicit action.
 */
export function continueHref(item: InspectionSummary): string {
  const base = `/agent/inspections/${item.id}`;
  if (item.status === "Review") return `${base}/review`;
  if (item.status !== "InProgress") return base;
  return item.roomsCompleted > 0 ? `${base}/descriptions` : `${base}/capture`;
}
