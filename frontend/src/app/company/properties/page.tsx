"use client";

import { useState } from "react";
import { PropertyTable, PropertyTableSkeleton } from "@/components/company/PropertyTable";
import { Icon } from "@/components/icons";
import { Button, Card, EmptyState, ErrorBanner, Input, LinkButton, PageHeader } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { useApi } from "@/lib/hooks";
import type { PropertySummary } from "@/lib/types";

/** Search is server-side (GET /api/properties?search=…), submitted explicitly so typing doesn't fire a request per key. */
function SearchBar({ onSearch, query }: { onSearch: (q: string) => void; query: string }) {
  const t = useT(companyPageMessages);
  const [search, setSearch] = useState(query);
  return (
    <form role="search" onSubmit={(e) => { e.preventDefault(); onSearch(search.trim()); }} className="flex gap-2">
      <div className="relative flex-1">
        <Icon name="search" className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-4" />
        <Input aria-label={t("search")} placeholder={t("searchPlaceholder")} value={search} onChange={(e) => setSearch(e.target.value)} className="pl-9" />
      </div>
      <Button type="submit" variant="secondary">{t("search")}</Button>
      {query && <Button type="button" variant="subtle" onClick={() => { setSearch(""); onSearch(""); }}>{t("clearSearch")}</Button>}
    </form>
  );
}

function PropertyResults({ data, loading, query }: { data: PropertySummary[] | null; loading: boolean; query: string }) {
  const t = useT(companyPageMessages);
  if (loading && !data) return <PropertyTableSkeleton />;
  if (!data) return null;
  if (data.length === 0) {
    return (
      <div className="p-4 sm:p-5">
        <EmptyState icon="building" title={query ? t("noMatchingProperties") : t("noPropertiesYet")}
          action={!query && <LinkButton href="/company/properties/new" icon="plus" size="sm">{t("addProperty")}</LinkButton>}>
          {t("noPropertiesHint")}
        </EmptyState>
      </div>
    );
  }
  return <PropertyTable items={data} />;
}

export default function PropertiesPage() {
  const t = useT(companyPageMessages);
  const [query, setQuery] = useState("");
  const { data, error, loading, reload } = useApi<PropertySummary[]>(`/api/properties${query ? `?search=${encodeURIComponent(query)}` : ""}`);

  return (
    <>
      <PageHeader title={t("properties")} subtitle={t("propertiesSubtitle")}
        actions={<LinkButton href="/company/properties/new" icon="plus">{t("addProperty")}</LinkButton>} />
      <ErrorBanner message={error} onRetry={reload} />
      <Card flush>
        <div className="flex flex-wrap items-center gap-3 border-b border-line p-3 sm:px-5">
          <div className="min-w-0 flex-1"><SearchBar query={query} onSearch={setQuery} /></div>
          {data && data.length > 0 && <span className="tabular text-label text-ink-3">{t("resultCount", { count: data.length })}</span>}
        </div>
        <PropertyResults data={data} loading={loading} query={query} />
      </Card>
    </>
  );
}
