"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { DEFAULT_ROOMS, RoomListEditor, type RoomDraft } from "@/components/RoomListEditor";
import { Button, Card, ErrorBanner, Field, Input, PageHeader, Select } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { humanize } from "@/lib/format";
import { PROPERTY_TYPES, type Property } from "@/lib/types";

export default function NewPropertyPage() {
  const router = useRouter();
  const [form, setForm] = useState({ addressLine1: "", addressLine2: "", city: "", postcode: "", country: "Ireland", propertyType: "House" });
  const [rooms, setRooms] = useState<RoomDraft[]>(DEFAULT_ROOMS);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [k]: e.target.value });

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const property = await post<Property>("/api/properties", {
        ...form, addressLine2: form.addressLine2 || null, rooms: rooms.map((r) => ({ roomType: r.roomType, name: r.name })),
      });
      router.replace(`/company/properties/${property.id}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title="Add property" back={{ href: "/company/properties", label: "Properties" }} />
      <ErrorBanner message={error} />
      <form onSubmit={submit} className="grid gap-4 lg:grid-cols-2">
        <Card title="Address">
          <div className="space-y-3">
            <Field label="Address line 1"><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
            <Field label="Address line 2 (optional)"><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="City"><Input required value={form.city} onChange={set("city")} /></Field>
              <Field label="Postcode / Eircode"><Input required value={form.postcode} onChange={set("postcode")} /></Field>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Country"><Input required value={form.country} onChange={set("country")} /></Field>
              <Field label="Property type">
                <Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</Select>
              </Field>
            </div>
          </div>
        </Card>
        <Card title={`Rooms (${rooms.length})`}>
          <p className="mb-3 text-sm text-slate-600">Add every room the inspector should record. You can change rooms later; published inspections keep their own copy.</p>
          <RoomListEditor rooms={rooms} onChange={setRooms} />
        </Card>
        <div className="lg:col-span-2"><Button type="submit" size="lg" loading={busy}>Create property</Button></div>
      </form>
    </>
  );
}
