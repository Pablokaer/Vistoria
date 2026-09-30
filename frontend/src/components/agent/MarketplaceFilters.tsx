"use client";

import { Icon } from "@/components/icons";
import { Button, Select } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { marketplaceMessages } from "@/i18n/messages/agent-marketplace";
import type { AvailableInspection, InspectionType, Visibility } from "@/lib/types";
import { distinct, EMPTY_QUERY, isFiltered, type MarketplaceQuery, type MarketplaceSort } from "./marketplace";

const SORTS: { value: MarketplaceSort; label: "sortNewest" | "sortAcceptBy" | "sortScheduled" | "sortRooms" }[] = [
  { value: "newest", label: "sortNewest" }, { value: "acceptBy", label: "sortAcceptBy" },
  { value: "scheduled", label: "sortScheduled" }, { value: "rooms", label: "sortRooms" },
];

function FilterSelect({ label, value, onChange, children }: { label: string; value: string; onChange: (v: string) => void; children: React.ReactNode }) {
  return (
    <label className="min-w-0 flex-[1_1_10rem]">
      <span className="sr-only">{label}</span>
      <Select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)} className="min-h-10">{children}</Select>
    </label>
  );
}

/**
 * Search + filter bar. Filter options come from the list itself, so nothing is offered that cannot match.
 * Example: `<MarketplaceFilters items={all} query={q} onChange={setQ} />`
 */
export function MarketplaceFilters({ items, query, onChange }: { items: AvailableInspection[]; query: MarketplaceQuery; onChange: (q: MarketplaceQuery) => void }) {
  const t = useT(marketplaceMessages);
  const { humanize } = useFormatters();
  // A filter with a single option cannot narrow anything; hide it until the list has variety.
  const types = distinct(items, "inspectionType");
  const propertyTypes = distinct(items, "propertyType");
  const visibilities = distinct(items, "visibility");
  const set = <K extends keyof MarketplaceQuery>(key: K, value: MarketplaceQuery[K]) => onChange({ ...query, [key]: value });
  return (
    <div className="rounded-lg border border-line bg-surface p-3 shadow-card">
      <div className="flex flex-wrap gap-2">
        <label className="relative block min-w-0 flex-[2_1_16rem]">
          <span className="sr-only">{t("searchLabel")}</span>
          <Icon name="search" className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-4" />
          <input type="search" value={query.search} onChange={(e) => set("search", e.target.value)} placeholder={t("searchPlaceholder")}
            className="block min-h-10 w-full rounded-md border border-line-strong bg-surface pl-9 pr-3 text-base text-ink shadow-card outline-none placeholder:text-ink-4 focus:border-brand focus:ring-3 focus:ring-brand/15 sm:text-body" />
        </label>
        {types.length > 1 && <FilterSelect label={t("typeLabel")} value={query.type} onChange={(v) => set("type", v as InspectionType | "all")}>
          <option value="all">{t("allTypes")}</option>
          {types.map((v) => <option key={v} value={v}>{humanize(v)}</option>)}
        </FilterSelect>}
        {propertyTypes.length > 1 && <FilterSelect label={t("propertyTypeLabel")} value={query.propertyType} onChange={(v) => set("propertyType", v)}>
          <option value="all">{t("allProperties")}</option>
          {propertyTypes.map((v) => <option key={v} value={v}>{humanize(v)}</option>)}
        </FilterSelect>}
        {visibilities.length > 1 && <FilterSelect label={t("visibilityLabel")} value={query.visibility} onChange={(v) => set("visibility", v as Visibility | "all")}>
          <option value="all">{t("allVisibility")}</option>
          {visibilities.map((v) => <option key={v} value={v}>{humanize(v)}</option>)}
        </FilterSelect>}
        <FilterSelect label={t("sortLabel")} value={query.sort} onChange={(v) => set("sort", v as MarketplaceSort)}>
          {SORTS.map((s) => <option key={s.value} value={s.value}>{t(s.label)}</option>)}
        </FilterSelect>
      </div>
      {isFiltered(query) && (
        <div className="mt-2 flex justify-end">
          <Button variant="subtle" size="sm" icon="close" onClick={() => onChange({ ...EMPTY_QUERY, sort: query.sort })}>{t("clearFilters")}</Button>
        </div>
      )}
    </div>
  );
}
