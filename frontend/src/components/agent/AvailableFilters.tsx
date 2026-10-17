"use client";

import { Icon } from "@/components/icons";
import { Button, Select } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { availableMessages } from "@/i18n/messages/agent-available";
import type { AvailableInspection, InspectionType } from "@/lib/types";
import { distinct, EMPTY_QUERY, isFiltered, type AvailableQuery, type AvailableSort } from "./availableQuery";

const SORTS: { value: AvailableSort; label: "sortNewest" | "sortAcceptBy" | "sortScheduled" | "sortRooms" }[] = [
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
 * Example: `<AvailableFilters items={all} query={q} onChange={setQ} />`
 */
export function AvailableFilters({ items, query, onChange }: { items: AvailableInspection[]; query: AvailableQuery; onChange: (q: AvailableQuery) => void }) {
  const t = useT(availableMessages);
  const { humanize } = useFormatters();
  // A filter with a single option cannot narrow anything; hide it until the list has variety.
  const types = distinct(items, "inspectionType");
  const propertyTypes = distinct(items, "propertyType");
  const set = <K extends keyof AvailableQuery>(key: K, value: AvailableQuery[K]) => onChange({ ...query, [key]: value });
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
        <FilterSelect label={t("sortLabel")} value={query.sort} onChange={(v) => set("sort", v as AvailableSort)}>
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
