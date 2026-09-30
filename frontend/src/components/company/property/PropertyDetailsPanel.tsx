"use client";

import { useState } from "react";
import { Button, Card, DefinitionList, Field, Input, Select } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import { put } from "@/lib/api";
import { PROPERTY_TYPES, type Property } from "@/lib/types";

export type Run = (fn: () => Promise<unknown>) => Promise<void>;

type AddressForm = Property & { addressLine2: string };

function AddressEditor({ property, onSaved, onCancel, run }: { property: Property; onSaved: (p: Property) => void; onCancel: () => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();
  const [form, setForm] = useState<AddressForm>({ ...property, addressLine2: property.addressLine2 ?? "" });
  const set = (k: keyof AddressForm) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [k]: e.target.value });
  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    void run(async () => { onSaved(await put<Property>(`/api/properties/${property.id}`, { ...form, addressLine2: form.addressLine2 || null })); });
  };
  return (
    <form className="space-y-3" onSubmit={submit}>
      <Field label={t("addressLine1")}><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
      <Field label={t("addressLine2")}><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
      <div className="grid grid-cols-2 gap-3">
        <Field label={t("city")}><Input required value={form.city} onChange={set("city")} /></Field>
        <Field label={t("postcode")}><Input required value={form.postcode} onChange={set("postcode")} /></Field>
        <Field label={t("country")}><Input required value={form.country} onChange={set("country")} /></Field>
        <Field label={t("type")}><Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((pt) => <option key={pt} value={pt}>{humanize(pt)}</option>)}</Select></Field>
      </div>
      <div className="flex justify-end gap-2 border-t border-line pt-3">
        <Button type="button" variant="subtle" onClick={onCancel}>{t("cancel")}</Button><Button type="submit">{t("save")}</Button>
      </div>
    </form>
  );
}

/** Address and type, read-only until "Edit". */
export function PropertyDetailsPanel({ property, onSaved, run }: { property: Property; onSaved: (p: Property) => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();
  const [editing, setEditing] = useState(false);
  if (editing) {
    return (
      <Card title={t("editProperty")}>
        <AddressEditor property={property} run={run} onCancel={() => setEditing(false)} onSaved={(p) => { onSaved(p); setEditing(false); }} />
      </Card>
    );
  }
  const address = [property.addressLine1, property.addressLine2, property.city, property.postcode, property.country].filter(Boolean).join(", ");
  return (
    <Card title={t("propertyDetails")} actions={<Button variant="ghost" size="sm" icon="edit" onClick={() => setEditing(true)}>{t("edit")}</Button>}>
      <DefinitionList items={[[t("address"), address], [t("type"), humanize(property.propertyType)]]} />
    </Card>
  );
}
