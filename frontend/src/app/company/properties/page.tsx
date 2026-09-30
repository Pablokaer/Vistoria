"use client";

import Link from "next/link";
import { useState } from "react";
import { EmptyState, ErrorBanner, Input, LinkButton, Loading, PageHeader } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { useApi } from "@/lib/hooks";
import type { PropertySummary } from "@/lib/types";

function PropertyCard({ property: p }: { property: PropertySummary }) {
  const t = useT(companyPageMessages);
  const { humanize } = useFormatters();
  return (
    <Link href={`/company/properties/${p.id}`} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand/40">
      <div className="font-medium text-slate-900">{p.addressLine1}</div>
      <div className="text-sm text-slate-600">{p.city} {p.postcode}</div>
      <div className="mt-3 flex flex-wrap gap-3 text-xs text-slate-500">
        <span>{humanize(p.propertyType)}</span><span>{t("roomCount", { count: p.roomCount })}</span>
        <span>{t("tenancyCount", { count: p.activeTenancies })}</span><span>{t("activeInspectionCount", { count: p.openInspections })}</span>
      </div>
    </Link>
  );
}

export default function PropertiesPage() {
  const t = useT(companyPageMessages);
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const { data, error, loading, reload } = useApi<PropertySummary[]>(`/api/properties${query ? `?search=${encodeURIComponent(query)}` : ""}`);

  return (
    <>
      <PageHeader title={t("properties")} actions={<LinkButton href="/company/properties/new">{t("addProperty")}</LinkButton>} />
      <form onSubmit={(e) => { e.preventDefault(); setQuery(search); }} className="mb-4 flex gap-2">
        <Input placeholder={t("searchPlaceholder")} value={search} onChange={(e) => setSearch(e.target.value)} />
      </form>
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && data.length === 0 ? (
        <EmptyState title={query ? t("noMatchingProperties") : t("noPropertiesYet")}>{t("noPropertiesHint")}</EmptyState>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {data?.map((p) => <PropertyCard key={p.id} property={p} />)}
        </div>
      )}
    </>
  );
}
