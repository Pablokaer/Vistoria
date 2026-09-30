"use client";

import Link from "next/link";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { FormSection, StickyFormActions } from "@/components/company/FormSection";
import { DEFAULT_SETTINGS, InspectionAssignmentFields, InspectionCreated, InspectionScheduleFields, InspectionTypePicker, settingsPayload, type InspectionSettings } from "@/components/InspectionForm";
import { RoomListEditor, useDefaultRooms, type RoomDraft } from "@/components/RoomListEditor";
import { Button, ErrorBanner, Field, Input, LinkButton, Notice, PageHeader, Select, Switch } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyNewMessages } from "@/i18n/messages/company-new";
import { PROPERTY_TYPES, type InspectionType, type Property, type PublishResult, type Tenancy } from "@/lib/types";

// A brand-new property has no Move In to compare with, so Move Out is not offered here.
const TYPES: InspectionType[] = ["MoveIn", "Periodic", "Other"];

type AddressForm = { addressLine1: string; addressLine2: string; city: string; postcode: string; country: string; propertyType: string };
type TenancyDates = { startDate: string; endDate: string };

function AddressFields({ form, onChange }: { form: AddressForm; onChange: (f: AddressForm) => void }) {
  const t = useT(companyNewMessages);
  const { humanize } = useFormatters();
  const set = (k: keyof AddressForm) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => onChange({ ...form, [k]: e.target.value });
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-6">
      <Field className="sm:col-span-6" label={t("addressLine1")}><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
      <Field className="sm:col-span-6" label={t("addressLine2")}><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
      <Field className="sm:col-span-3" label={t("city")}><Input required value={form.city} onChange={set("city")} /></Field>
      <Field className="sm:col-span-3" label={t("postcode")}><Input required value={form.postcode} onChange={set("postcode")} /></Field>
      <Field className="sm:col-span-3" label={t("country")}><Input required value={form.country} onChange={set("country")} /></Field>
      <Field className="sm:col-span-3" label={t("propertyType")}>
        <Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((pt) => <option key={pt} value={pt}>{humanize(pt)}</option>)}</Select>
      </Field>
    </div>
  );
}

function TenancyDateFields({ value, onChange, required, locked }: { value: TenancyDates; onChange: (v: TenancyDates) => void; required: boolean; locked: boolean }) {
  const t = useT(companyNewMessages);
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <Field label={required ? t("tenancyStartDate") : t("tenancyStartDateOptional")} hint={t("tenancyStartDateHint")}>
        <Input type="date" required={required} disabled={locked} value={value.startDate} onChange={(e) => onChange({ ...value, startDate: e.target.value })} />
      </Field>
      <Field label={t("tenancyEndDate")}>
        <Input type="date" disabled={locked} value={value.endDate} onChange={(e) => onChange({ ...value, endDate: e.target.value })} />
      </Field>
    </div>
  );
}

export default function NewPropertyPage() {
  const router = useRouter();
  const t = useT(companyNewMessages);
  const [form, setForm] = useState<AddressForm>({ addressLine1: "", addressLine2: "", city: "", postcode: "", country: t("defaultCountry"), propertyType: "House" });
  const [rooms, setRooms] = useState<RoomDraft[]>(useDefaultRooms());
  const [withInspection, setWithInspection] = useState(false);
  const [type, setType] = useState<InspectionType>("MoveIn");
  const [tenancy, setTenancy] = useState<TenancyDates>({ startDate: "", endDate: "" });
  const [settings, setSettings] = useState<InspectionSettings>(DEFAULT_SETTINGS);
  // Kept across attempts so a failed inspection step can be retried without duplicating the property or tenancy.
  const [property, setProperty] = useState<Property | null>(null);
  const [createdTenancy, setCreatedTenancy] = useState<Tenancy | null>(null);
  const [result, setResult] = useState<PublishResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    let saved = property;
    try {
      saved ??= await post<Property>("/api/properties", {
        ...form, addressLine2: form.addressLine2 || null, rooms: rooms.map((r) => ({ roomType: r.roomType, name: r.name })),
      });
      setProperty(saved);
      if (!withInspection) { router.replace(`/company/properties/${saved.id}`); return; }

      let tenancyToUse = createdTenancy;
      if (!tenancyToUse && tenancy.startDate) {
        tenancyToUse = await post<Tenancy>("/api/tenancies", { propertyId: saved.id, startDate: tenancy.startDate, endDate: tenancy.endDate || null, reference: null });
        setCreatedTenancy(tenancyToUse);
      }
      setResult(await post<PublishResult>("/api/inspections", {
        propertyId: saved.id, tenancyId: tenancyToUse?.id ?? null, inspectionType: type, comparisonInspectionId: null, ...settingsPayload(settings),
      }));
    } catch (err) {
      setError(saved ? t("inspectionFailedAfterProperty", { error: errorMessage(err) }) : errorMessage(err));
      setBusy(false);
    }
  }

  if (result && property) {
    return <InspectionCreated result={result} extra={<LinkButton variant="secondary" href={`/company/properties/${property.id}`}>{t("goToProperty")}</LinkButton>} />;
  }

  const submitLabel = !withInspection ? t("createProperty") : settings.publishNow ? t("createPropertyAndPublish") : t("createPropertyAndDraft");
  const summary = !withInspection ? t("summaryPropertyOnly", { count: rooms.length })
    : `${t("summaryWithInspection")} ${settings.publishNow ? t("summaryPublish") : t("summaryDraft")}`;

  return (
    <div className="mx-auto max-w-page">
      <PageHeader title={t("addProperty")} back={{ href: "/company/properties", label: t("properties") }} />
      <ErrorBanner message={error} />
      {property && (
        <Notice tone="info">
          {t("propertyAlreadySaved")} <Link className="font-medium underline" href={`/company/properties/${property.id}`}>{t("goToProperty")}</Link>
        </Notice>
      )}
      <form onSubmit={submit} className="space-y-8">
        <fieldset disabled={!!property} className="space-y-8">
          <FormSection step={1} title={t("stepAddress")} description={t("stepAddressHint")}><AddressFields form={form} onChange={setForm} /></FormSection>
          <FormSection step={2} title={t("rooms", { count: rooms.length })} description={t("roomsHint")}><RoomListEditor rooms={rooms} onChange={setRooms} /></FormSection>
        </fieldset>
        <FormSection step={3} title={t("stepFirstInspection")} description={t("stepFirstInspectionHint")} muted={!withInspection}>
          <Switch checked={withInspection} onChange={setWithInspection} label={t("alsoCreateInspection")} description={t("alsoCreateInspectionHint")} />
          {withInspection && (
            <div className="mt-5 space-y-6 border-t border-line pt-5 animate-fade-in">
              <InspectionTypePicker types={TYPES} value={type} onChange={setType} />
              <TenancyDateFields value={tenancy} onChange={setTenancy} required={type === "MoveIn"} locked={!!createdTenancy} />
              <InspectionAssignmentFields value={settings} onChange={setSettings} />
              <InspectionScheduleFields value={settings} onChange={setSettings} />
            </div>
          )}
        </FormSection>
        <StickyFormActions summary={summary}>
          <LinkButton href="/company/properties" variant="subtle">{t("cancel")}</LinkButton>
          <Button type="submit" size="lg" loading={busy}>{submitLabel}</Button>
        </StickyFormActions>
      </form>
    </div>
  );
}
