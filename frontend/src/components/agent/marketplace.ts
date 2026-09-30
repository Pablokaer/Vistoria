// Client-side search, filters and sorting over GET /api/agent/available (the API returns the whole open list).
import type { AvailableInspection, InspectionType, Visibility } from "@/lib/types";

export type MarketplaceSort = "newest" | "acceptBy" | "scheduled" | "rooms";

export interface MarketplaceQuery {
  search: string;
  type: InspectionType | "all";
  propertyType: string | "all";
  visibility: Visibility | "all";
  sort: MarketplaceSort;
}

export const EMPTY_QUERY: MarketplaceQuery = { search: "", type: "all", propertyType: "all", visibility: "all", sort: "newest" };

/** True when any filter narrows the list (sorting alone does not). */
export function isFiltered(q: MarketplaceQuery): boolean {
  return q.search.trim() !== "" || q.type !== "all" || q.propertyType !== "all" || q.visibility !== "all";
}

function matchesSearch(item: AvailableInspection, search: string): boolean {
  const needle = search.trim().toLowerCase();
  if (!needle) return true;
  const haystack = [item.city, item.postcodeArea, item.companyName, ...item.roomNames].join(" ").toLowerCase();
  return needle.split(/\s+/).every((word) => haystack.includes(word));
}

// Missing dates sort last so "soonest" never puts undated items first.
const byDateAsc = (a: string | null, b: string | null) => (a ?? "9999").localeCompare(b ?? "9999");

const SORTERS: Record<MarketplaceSort, (a: AvailableInspection, b: AvailableInspection) => number> = {
  newest: (a, b) => (b.publishedAt ?? "").localeCompare(a.publishedAt ?? ""),
  acceptBy: (a, b) => byDateAsc(a.acceptBy, b.acceptBy),
  scheduled: (a, b) => byDateAsc(a.scheduledDate, b.scheduledDate),
  rooms: (a, b) => b.roomCount - a.roomCount,
};

/**
 * Applies the query. Example: `applyQuery(items, { ...EMPTY_QUERY, type: "MoveOut", sort: "acceptBy" })`.
 */
export function applyQuery(items: AvailableInspection[], q: MarketplaceQuery): AvailableInspection[] {
  return items
    .filter((i) => matchesSearch(i, q.search))
    .filter((i) => q.type === "all" || i.inspectionType === q.type)
    .filter((i) => q.propertyType === "all" || i.propertyType === q.propertyType)
    .filter((i) => q.visibility === "all" || i.visibility === q.visibility)
    .sort(SORTERS[q.sort]);
}

/** Distinct values present in the list, so filters only offer options that can match. */
export function distinct<K extends "inspectionType" | "propertyType" | "visibility">(items: AvailableInspection[], key: K): AvailableInspection[K][] {
  return [...new Set(items.map((i) => i[key]))];
}
