"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { DEFAULT_SETTINGS, InspectionCreated, InspectionSettingsFields, InspectionTypePicker, settingsPayload, type InspectionSettings } from "@/components/InspectionForm";
import { Button, Card, ErrorBanner, Field, Notice, PageHeader, Select } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { formatDate } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary, InspectionType, PropertySummary, PublishResult, Tenancy } from "@/lib/types";

const TYPES: InspectionType[] = ["MoveIn", "MoveOut", "Periodic", "Other"];

function NewInspectionForm() {
  const params = useSearchParams();
  const properties = useApi<PropertySummary[]>("/api/properties");
  const [propertyChoice, setPropertyId] = useState(params.get("propertyId") ?? "");
  const propertyId = propertyChoice || (properties.data?.length === 1 ? properties.data[0].id : "");
  const tenancies = useApi<Tenancy[]>(propertyId ? `/api/properties/${propertyId}/tenancies` : null);
  const candidates = useApi<InspectionSummary[]>(propertyId ? `/api/properties/${propertyId}/comparison-candidates` : null);

  const [type, setType] = useState<InspectionType>("MoveIn");
  // null = "use the sensible default"; derived below instead of syncing state in effects.
  const [tenancyChoice, setTenancyId] = useState<string | null>(null);
  const [comparisonChoice, setComparisonId] = useState<string | null>(null);
  const [settings, setSettings] = useState<InspectionSettings>(DEFAULT_SETTINGS);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<PublishResult | null>(null);

  const defaultTenancy = (tenancies.data ?? []).find((t) => t.status === "Active" || t.status === "Upcoming") ?? tenancies.data?.[0];
  const tenancyId = tenancyChoice ?? defaultTenancy?.id ?? "";
  // Baselines must come from the same tenancy (enforced by the API).
  const tenancyCandidates = (candidates.data ?? []).filter((c) => c.tenancyId === (tenancyId || null));
  const defaultComparison = type === "MoveOut" ? tenancyCandidates[0]?.id ?? "" : "";
  const comparisonId = type === "MoveOut" || type === "Periodic" ? comparisonChoice ?? defaultComparison : "";

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      setResult(await post<PublishResult>("/api/inspections", {
        propertyId, tenancyId: tenancyId || null, inspectionType: type, comparisonInspectionId: comparisonId || null, ...settingsPayload(settings),
      }));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (result) return <InspectionCreated result={result} />;

  const tenancyRequired = type === "MoveIn" || type === "MoveOut";
  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader title="New inspection" back={{ href: "/company", label: "Dashboard" }} />
      <ErrorBanner message={error ?? properties.error} />
      <Card>
        <form onSubmit={submit} className="space-y-4">
          <Field label="Property">
            <Select required value={propertyId} onChange={(e) => { setPropertyId(e.target.value); setTenancyId(null); setComparisonId(null); }}>
              <option value="">Select a property…</option>
              {properties.data?.map((p) => <option key={p.id} value={p.id}>{p.addressLine1}, {p.city} ({p.roomCount} rooms)</option>)}
            </Select>
          </Field>
          {properties.data?.length === 0 && <Notice>You have no properties yet. <Link className="underline" href="/company/properties/new">Add one first.</Link></Notice>}
          <InspectionTypePicker types={TYPES} value={type} onChange={setType} />
          <Field label={`Tenancy${tenancyRequired ? "" : " (optional)"}`} hint={propertyId && tenancies.data?.length === 0 ? "This property has no tenancy yet — create one on the property page." : undefined}>
            <Select value={tenancyId} required={tenancyRequired} onChange={(e) => setTenancyId(e.target.value)}>
              <option value="">{tenancyRequired ? "Select a tenancy…" : "No tenancy"}</option>
              {tenancies.data?.map((t) => <option key={t.id} value={t.id}>{formatDate(t.startDate)} – {t.endDate ? formatDate(t.endDate) : "ongoing"} · {t.members.map((m) => m.fullName).join(", ") || "no tenants"}</option>)}
            </Select>
          </Field>
          {(type === "MoveOut" || type === "Periodic") && (
            <Field label="Compare with (Move In baseline)" hint="The agent will see the Move In record of each room while inspecting.">
              <Select value={comparisonId} onChange={(e) => setComparisonId(e.target.value)}>
                <option value="">No comparison</option>
                {tenancyCandidates.map((c) => <option key={c.id} value={c.id}>Move In · completed {formatDate(c.completedAt)} · {c.agentName}</option>)}
              </Select>
            </Field>
          )}
          <InspectionSettingsFields value={settings} onChange={setSettings} />
          <Button type="submit" size="lg" loading={busy} disabled={!propertyId || tenancies.loading || candidates.loading}>{settings.publishNow ? "Create and publish" : "Save draft"}</Button>
        </form>
      </Card>
    </div>
  );
}

export default function NewInspectionPage() {
  return <Suspense><NewInspectionForm /></Suspense>;
}
