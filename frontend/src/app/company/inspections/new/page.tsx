"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { DEFAULT_SETTINGS, InspectionCreated, InspectionSettingsFields, InspectionTypePicker, settingsPayload, type InspectionSettings } from "@/components/InspectionForm";
import { Button, Card, ErrorBanner, Field, Notice, PageHeader, Select } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyNewMessages } from "@/i18n/messages/company-new";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary, InspectionType, PropertySummary, PublishResult, Tenancy } from "@/lib/types";

const TYPES: InspectionType[] = ["MoveIn", "MoveOut", "Periodic", "Other"];

function NewInspectionForm() {
  const params = useSearchParams();
  const t = useT(companyNewMessages);
  const { formatDate } = useFormatters();
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

  const defaultTenancy = (tenancies.data ?? []).find((tn) => tn.status === "Active" || tn.status === "Upcoming") ?? tenancies.data?.[0];
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
      <PageHeader title={t("newInspection")} back={{ href: "/company", label: t("dashboard") }} />
      <ErrorBanner message={error ?? properties.error} />
      <Card>
        <form onSubmit={submit} className="space-y-4">
          <Field label={t("property")}>
            <Select required value={propertyId} onChange={(e) => { setPropertyId(e.target.value); setTenancyId(null); setComparisonId(null); }}>
              <option value="">{t("selectProperty")}</option>
              {properties.data?.map((p) => <option key={p.id} value={p.id}>{t("propertyOption", { address: p.addressLine1, city: p.city, rooms: p.roomCount })}</option>)}
            </Select>
          </Field>
          {properties.data?.length === 0 && <Notice>{t("noPropertiesYet")} <Link className="underline" href="/company/properties/new">{t("addOneFirst")}</Link></Notice>}
          <InspectionTypePicker types={TYPES} value={type} onChange={setType} />
          <Field label={tenancyRequired ? t("tenancy") : t("tenancyOptional")} hint={propertyId && tenancies.data?.length === 0 ? t("noTenancyHint") : undefined}>
            <Select value={tenancyId} required={tenancyRequired} onChange={(e) => setTenancyId(e.target.value)}>
              <option value="">{tenancyRequired ? t("selectTenancy") : t("noTenancy")}</option>
              {tenancies.data?.map((tn) => <option key={tn.id} value={tn.id}>{formatDate(tn.startDate)} – {tn.endDate ? formatDate(tn.endDate) : t("ongoing")} · {tn.members.map((m) => m.fullName).join(", ") || t("noTenants")}</option>)}
            </Select>
          </Field>
          {(type === "MoveOut" || type === "Periodic") && (
            <Field label={t("compareWith")} hint={t("compareWithHint")}>
              <Select value={comparisonId} onChange={(e) => setComparisonId(e.target.value)}>
                <option value="">{t("noComparison")}</option>
                {tenancyCandidates.map((c) => <option key={c.id} value={c.id}>{t("comparisonOption", { date: formatDate(c.completedAt), agent: c.agentName ?? "" })}</option>)}
              </Select>
            </Field>
          )}
          <InspectionSettingsFields value={settings} onChange={setSettings} />
          <Button type="submit" size="lg" loading={busy} disabled={!propertyId || tenancies.loading || candidates.loading}>{settings.publishNow ? t("createAndPublish") : t("saveDraft")}</Button>
        </form>
      </Card>
    </div>
  );
}

export default function NewInspectionPage() {
  return <Suspense><NewInspectionForm /></Suspense>;
}
