"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { FormSection, StickyFormActions } from "@/components/company/FormSection";
import { DEFAULT_SETTINGS, InspectionAssignmentFields, InspectionCreated, InspectionScheduleFields, InspectionTypePicker, settingsPayload, type InspectionSettings } from "@/components/InspectionForm";
import { Button, ErrorBanner, Field, LinkButton, Notice, PageHeader, Select } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyNewMessages } from "@/i18n/messages/company-new";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary, InspectionType, PropertySummary, PublishResult, Tenancy } from "@/lib/types";

const TYPES: InspectionType[] = ["MoveIn", "MoveOut", "Periodic", "Other"];

interface PropertyStepProps {
  properties: PropertySummary[] | null; propertyId: string; onProperty: (id: string) => void;
  tenancies: Tenancy[] | null; tenancyId: string; onTenancy: (id: string) => void; tenancyRequired: boolean;
  showComparison: boolean; candidates: InspectionSummary[]; comparisonId: string; onComparison: (id: string) => void;
}

function TenancySelect({ tenancies, value, onChange, required, propertyChosen }:
  { tenancies: Tenancy[] | null; value: string; onChange: (id: string) => void; required: boolean; propertyChosen: boolean }) {
  const t = useT(companyNewMessages);
  const { formatDate } = useFormatters();
  return (
    <Field label={required ? t("tenancy") : t("tenancyOptional")} hint={propertyChosen && tenancies?.length === 0 ? t("noTenancyHint") : undefined}>
      <Select value={value} required={required} onChange={(e) => onChange(e.target.value)}>
        <option value="">{required ? t("selectTenancy") : t("noTenancy")}</option>
        {tenancies?.map((tn) => <option key={tn.id} value={tn.id}>{formatDate(tn.startDate)} – {tn.endDate ? formatDate(tn.endDate) : t("ongoing")} · {tn.members.map((m) => m.fullName).join(", ") || t("noTenants")}</option>)}
      </Select>
    </Field>
  );
}

function PropertyStep(p: PropertyStepProps) {
  const t = useT(companyNewMessages);
  const { formatDate } = useFormatters();
  return (
    <div className="space-y-4">
      <Field label={t("property")}>
        <Select required value={p.propertyId} onChange={(e) => p.onProperty(e.target.value)}>
          <option value="">{t("selectProperty")}</option>
          {p.properties?.map((pr) => <option key={pr.id} value={pr.id}>{t("propertyOption", { address: pr.addressLine1, city: pr.city, rooms: pr.roomCount })}</option>)}
        </Select>
      </Field>
      {p.properties?.length === 0 && <Notice>{t("noPropertiesYet")} <Link className="font-medium underline" href="/company/properties/new">{t("addOneFirst")}</Link></Notice>}
      <TenancySelect tenancies={p.tenancies} value={p.tenancyId} onChange={p.onTenancy} required={p.tenancyRequired} propertyChosen={!!p.propertyId} />
      {p.showComparison && (
        <Field label={t("compareWith")} hint={t("compareWithHint")}>
          <Select value={p.comparisonId} onChange={(e) => p.onComparison(e.target.value)}>
            <option value="">{t("noComparison")}</option>
            {p.candidates.map((c) => <option key={c.id} value={c.id}>{t("comparisonOption", { date: formatDate(c.completedAt), agent: c.agentName ?? "" })}</option>)}
          </Select>
        </Field>
      )}
    </div>
  );
}

function NewInspectionForm() {
  const params = useSearchParams();
  const t = useT(companyNewMessages);
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
  const showComparison = type === "MoveOut" || type === "Periodic";
  const comparisonId = showComparison ? comparisonChoice ?? defaultComparison : "";

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

  return (
    <div className="mx-auto max-w-page">
      <PageHeader title={t("newInspection")} back={{ href: "/company", label: t("dashboard") }} />
      <ErrorBanner message={error ?? properties.error} />
      <form onSubmit={submit} className="space-y-8">
        <FormSection step={1} title={t("stepType")} description={t("stepTypeHint")}>
          <InspectionTypePicker types={TYPES} value={type} onChange={setType} />
        </FormSection>
        <FormSection step={2} title={t("stepProperty")} description={t("stepPropertyHint")}>
          <PropertyStep properties={properties.data} propertyId={propertyId} onProperty={(id) => { setPropertyId(id); setTenancyId(null); setComparisonId(null); }}
            tenancies={tenancies.data} tenancyId={tenancyId} onTenancy={setTenancyId} tenancyRequired={type === "MoveIn" || type === "MoveOut"}
            showComparison={showComparison} candidates={tenancyCandidates} comparisonId={comparisonId} onComparison={setComparisonId} />
        </FormSection>
        <FormSection step={3} title={t("stepAssignment")} description={t("stepAssignmentHint")}>
          <InspectionAssignmentFields value={settings} onChange={setSettings} />
        </FormSection>
        <FormSection step={4} title={t("stepSchedule")} description={t("stepScheduleHint")}>
          <InspectionScheduleFields value={settings} onChange={setSettings} />
        </FormSection>
        <StickyFormActions summary={settings.publishNow ? t("summaryPublish") : t("summaryDraft")}>
          <LinkButton href="/company" variant="subtle">{t("cancel")}</LinkButton>
          <Button type="submit" size="lg" loading={busy} disabled={!propertyId || tenancies.loading || candidates.loading}>{settings.publishNow ? t("createAndPublish") : t("saveDraft")}</Button>
        </StickyFormActions>
      </form>
    </div>
  );
}

export default function NewInspectionPage() {
  return <Suspense><NewInspectionForm /></Suspense>;
}
