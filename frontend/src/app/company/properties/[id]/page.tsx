"use client";

import { useParams } from "next/navigation";
import { useState } from "react";
import { PropertyDetailsPanel } from "@/components/company/property/PropertyDetailsPanel";
import { PropertyInspectionsPanel } from "@/components/company/property/PropertyInspectionsPanel";
import { PropertyRoomsPanel } from "@/components/company/property/PropertyRoomsPanel";
import { PropertyTenanciesPanel } from "@/components/company/property/PropertyTenanciesPanel";
import { Icon } from "@/components/icons";
import { ErrorBanner, LinkButton, PageHeader, PageSkeleton } from "@/components/ui";
import { errorMessage } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary, Property, Tenancy } from "@/lib/types";

function Fact({ icon, children }: { icon: "building" | "rooms" | "users"; children: React.ReactNode }) {
  return <span className="inline-flex items-center gap-1.5 text-label text-ink-3"><Icon name={icon} className="h-4 w-4 text-ink-4" />{children}</span>;
}

/**
 * One property: operational work first (inspections, then rooms) in the main column; who lives there and the
 * address in the side column.
 */
export default function PropertyDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const property = useApi<Property>(`/api/properties/${id}`);
  const tenancies = useApi<Tenancy[]>(`/api/properties/${id}/tenancies`);
  const inspections = useApi<InspectionSummary[]>(`/api/inspections?propertyId=${id}`);
  const [error, setError] = useState<string | null>(null);
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();

  const run = async (fn: () => Promise<unknown>) => {
    setError(null);
    try { await fn(); } catch (e) { setError(errorMessage(e)); }
  };

  if (property.loading && !property.data) return <PageSkeleton />;
  if (!property.data) return <ErrorBanner message={property.error ?? t("notFound")} onRetry={property.reload} />;
  const p = property.data;

  return (
    <>
      <PageHeader title={p.addressLine1} subtitle={[p.addressLine2, p.city, p.postcode, p.country].filter(Boolean).join(", ")}
        back={{ href: "/company/properties", label: t("properties") }}
        meta={<>
          <Fact icon="building">{humanize(p.propertyType)}</Fact>
          <Fact icon="rooms">{t("roomCount", { count: p.rooms.length })}</Fact>
          <Fact icon="users">{t("tenancyCount", { count: tenancies.data?.length ?? 0 })}</Fact>
        </>}
        actions={<LinkButton href={`/company/inspections/new?propertyId=${p.id}`} icon="plus">{t("newInspection")}</LinkButton>} />
      <ErrorBanner message={error} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="space-y-6">
          <PropertyInspectionsPanel propertyId={p.id} items={inspections.data} loading={inspections.loading} />
          <PropertyRoomsPanel property={p} onChanged={(np) => property.setData(np)} run={run} />
        </div>
        <div className="space-y-6">
          <PropertyTenanciesPanel propertyId={p.id} tenancies={tenancies.data ?? []} reload={tenancies.reload} run={run} />
          <PropertyDetailsPanel property={p} onSaved={(np) => property.setData(np)} run={run} />
        </div>
      </div>
    </>
  );
}
