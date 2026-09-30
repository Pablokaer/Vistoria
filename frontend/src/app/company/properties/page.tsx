"use client";

import Link from "next/link";
import { useState } from "react";
import { EmptyState, ErrorBanner, Input, LinkButton, Loading, PageHeader } from "@/components/ui";
import { humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { PropertySummary } from "@/lib/types";

export default function PropertiesPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState("");
  const { data, error, loading, reload } = useApi<PropertySummary[]>(`/api/properties${query ? `?search=${encodeURIComponent(query)}` : ""}`);

  return (
    <>
      <PageHeader title="Properties" actions={<LinkButton href="/company/properties/new">Add property</LinkButton>} />
      <form onSubmit={(e) => { e.preventDefault(); setQuery(search); }} className="mb-4 flex gap-2">
        <Input placeholder="Search by address, city or postcode" value={search} onChange={(e) => setSearch(e.target.value)} />
      </form>
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && data.length === 0 ? (
        <EmptyState title={query ? "No matching properties" : "No properties yet"}>Add your first property and its rooms.</EmptyState>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {data?.map((p) => (
            <Link key={p.id} href={`/company/properties/${p.id}`} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand/40">
              <div className="font-medium text-slate-900">{p.addressLine1}</div>
              <div className="text-sm text-slate-600">{p.city} {p.postcode}</div>
              <div className="mt-3 flex flex-wrap gap-3 text-xs text-slate-500">
                <span>{humanize(p.propertyType)}</span><span>{p.roomCount} rooms</span>
                <span>{p.activeTenancies} tenancies</span><span>{p.openInspections} active inspections</span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </>
  );
}
