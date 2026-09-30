"use client";

import Link from "next/link";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { DEFAULT_SETTINGS, InspectionCreated, InspectionSettingsFields, InspectionTypePicker, settingsPayload, type InspectionSettings } from "@/components/InspectionForm";
import { RoomListEditor, useDefaultRooms, type RoomDraft } from "@/components/RoomListEditor";
import { Button, Card, ErrorBanner, Field, Input, LinkButton, Notice, PageHeader, Select } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyNewMessages } from "@/i18n/messages/company-new";
import { PROPERTY_TYPES, type InspectionType, type Property, type PublishResult, type Tenancy } from "@/lib/types";

// A brand-new property has no Move In to compare with, so Move Out is not offered here.
const TYPES: InspectionType[] = ["MoveIn", "Periodic", "Other"];

export default function NewPropertyPage() {
  const router = useRouter();
  const t = useT(companyNewMessages);
  const { humanize } = useFormatters();
  const [form, setForm] = useState({ addressLine1: "", addressLine2: "", city: "", postcode: "", country: t("defaultCountry"), propertyType: "House" });
  const [rooms, setRooms] = useState<RoomDraft[]>(useDefaultRooms());
  const [withInspection, setWithInspection] = useState(false);
  const [type, setType] = useState<InspectionType>("MoveIn");
  const [tenancy, setTenancy] = useState({ startDate: "", endDate: "" });
  const [settings, setSettings] = useState<InspectionSettings>(DEFAULT_SETTINGS);
  // Kept across attempts so a failed inspection step can be retried without duplicating the property or tenancy.
  const [property, setProperty] = useState<Property | null>(null);
  const [createdTenancy, setCreatedTenancy] = useState<Tenancy | null>(null);
  const [result, setResult] = useState<PublishResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [k]: e.target.value });

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

  const tenancyRequired = type === "MoveIn";
  const submitLabel = !withInspection ? t("createProperty") : settings.publishNow ? t("createPropertyAndPublish") : t("createPropertyAndDraft");

  return (
    <>
      <PageHeader title={t("addProperty")} back={{ href: "/company/properties", label: t("properties") }} />
      <ErrorBanner message={error} />
      {property && (
        <Notice tone="info">
          {t("propertyAlreadySaved")} <Link className="underline" href={`/company/properties/${property.id}`}>{t("goToProperty")}</Link>
        </Notice>
      )}
      <form onSubmit={submit} className="grid gap-4 lg:grid-cols-2">
        <fieldset disabled={!!property} className="contents">
          <Card title={t("address")}>
            <div className="space-y-3">
              <Field label={t("addressLine1")}><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
              <Field label={t("addressLine2")}><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
              <div className="grid grid-cols-2 gap-3">
                <Field label={t("city")}><Input required value={form.city} onChange={set("city")} /></Field>
                <Field label={t("postcode")}><Input required value={form.postcode} onChange={set("postcode")} /></Field>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <Field label={t("country")}><Input required value={form.country} onChange={set("country")} /></Field>
                <Field label={t("propertyType")}>
                  <Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((pt) => <option key={pt} value={pt}>{humanize(pt)}</option>)}</Select>
                </Field>
              </div>
            </div>
          </Card>
          <Card title={t("rooms", { count: rooms.length })}>
            <p className="mb-3 text-sm text-slate-600">{t("roomsHint")}</p>
            <RoomListEditor rooms={rooms} onChange={setRooms} />
          </Card>
        </fieldset>

        <div className="lg:col-span-2">
          <Card>
            <label className="flex cursor-pointer items-center gap-3">
              <input type="checkbox" className="h-5 w-5 accent-brand" checked={withInspection} onChange={(e) => setWithInspection(e.target.checked)} />
              <span><span className="block font-semibold text-slate-900">{t("alsoCreateInspection")}</span>
                <span className="text-sm text-slate-600">{t("alsoCreateInspectionHint")}</span></span>
            </label>
            <fieldset disabled={!withInspection} className={`mt-4 space-y-4 border-t border-slate-100 pt-4 ${withInspection ? "" : "opacity-50"}`}>
              <InspectionTypePicker types={TYPES} value={type} onChange={setType} />
              <div className="grid gap-3 sm:grid-cols-2">
                <Field label={tenancyRequired ? t("tenancyStartDate") : t("tenancyStartDateOptional")} hint={t("tenancyStartDateHint")}>
                  <Input type="date" required={withInspection && tenancyRequired} disabled={!!createdTenancy} value={tenancy.startDate}
                    onChange={(e) => setTenancy({ ...tenancy, startDate: e.target.value })} />
                </Field>
                <Field label={t("tenancyEndDate")}>
                  <Input type="date" disabled={!!createdTenancy} value={tenancy.endDate} onChange={(e) => setTenancy({ ...tenancy, endDate: e.target.value })} />
                </Field>
              </div>
              <InspectionSettingsFields value={settings} onChange={setSettings} />
            </fieldset>
          </Card>
        </div>

        <div className="lg:col-span-2"><Button type="submit" size="lg" loading={busy}>{submitLabel}</Button></div>
      </form>
    </>
  );
}
