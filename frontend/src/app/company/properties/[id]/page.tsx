"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, CopyField, EmptyState, ErrorBanner, Field, Input, LinkButton, Loading, Notice, PageHeader, Select } from "@/components/ui";
import { del, errorMessage, post, put } from "@/lib/api";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import { useApi } from "@/lib/hooks";
import { PROPERTY_TYPES, ROOM_TYPES, type InspectionSummary, type Property, type RoomType, type Tenancy, type TenantInvitation } from "@/lib/types";

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

  if (property.loading && !property.data) return <Loading />;
  if (!property.data) return <ErrorBanner message={property.error ?? t("notFound")} onRetry={property.reload} />;
  const p = property.data;

  return (
    <>
      <PageHeader title={p.addressLine1} subtitle={`${[p.addressLine2, p.city, p.postcode, p.country].filter(Boolean).join(", ")} · ${humanize(p.propertyType)}`}
        back={{ href: "/company/properties", label: t("properties") }}
        actions={<LinkButton href={`/company/inspections/new?propertyId=${p.id}`}>{t("newInspection")}</LinkButton>} />
      <ErrorBanner message={error} />
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-4">
          <DetailsCard property={p} onSaved={(np) => property.setData(np)} run={run} />
          <RoomsCard property={p} onChanged={(np) => property.setData(np)} run={run} />
        </div>
        <div className="space-y-4">
          <TenanciesCard propertyId={p.id} tenancies={tenancies.data ?? []} reload={tenancies.reload} run={run} />
          <Card title={t("inspections")} actions={<LinkButton variant="ghost" href={`/company/inspections/new?propertyId=${p.id}`}>{t("newShort")}</LinkButton>}>
            {!inspections.data?.length ? <EmptyState title={t("noInspections")} /> : (
              <ul className="divide-y divide-slate-100">
                {inspections.data.map((i) => (
                  <li key={i.id}>
                    <Link href={`/company/inspections/${i.id}`} className="flex items-center justify-between gap-2 py-2.5 hover:bg-slate-50">
                      <span className="text-sm"><span className="font-medium">{humanize(i.inspectionType)}</span>
                        <span className="text-slate-500">{t("inspectionRow", { completed: i.roomsCompleted, total: i.roomsTotal, agent: i.agentName ?? t("unassigned") })}</span></span>
                      <Badge value={i.status} />
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      </div>
    </>
  );
}

type Run = (fn: () => Promise<unknown>) => Promise<void>;

function DetailsCard({ property, onSaved, run }: { property: Property; onSaved: (p: Property) => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({ ...property, addressLine2: property.addressLine2 ?? "" });
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [k]: e.target.value });
  if (!editing) {
    return (
      <Card title={t("propertyDetails")} actions={<Button variant="ghost" onClick={() => { setForm({ ...property, addressLine2: property.addressLine2 ?? "" }); setEditing(true); }}>{t("edit")}</Button>}>
        <p className="text-sm text-slate-700">{[property.addressLine1, property.addressLine2, property.city, property.postcode, property.country].filter(Boolean).join(", ")}</p>
      </Card>
    );
  }
  return (
    <Card title={t("editProperty")}>
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); void run(async () => {
        onSaved(await put<Property>(`/api/properties/${property.id}`, { ...form, addressLine2: form.addressLine2 || null }));
        setEditing(false);
      }); }}>
        <Field label={t("addressLine1")}><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
        <Field label={t("addressLine2")}><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label={t("city")}><Input required value={form.city} onChange={set("city")} /></Field>
          <Field label={t("postcode")}><Input required value={form.postcode} onChange={set("postcode")} /></Field>
          <Field label={t("country")}><Input required value={form.country} onChange={set("country")} /></Field>
          <Field label={t("type")}><Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((pt) => <option key={pt} value={pt}>{humanize(pt)}</option>)}</Select></Field>
        </div>
        <div className="flex gap-2"><Button type="submit">{t("save")}</Button><Button type="button" variant="secondary" onClick={() => setEditing(false)}>{t("cancel")}</Button></div>
      </form>
    </Card>
  );
}

function RoomsCard({ property, onChanged, run }: { property: Property; onChanged: (p: Property) => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();
  const [newType, setNewType] = useState<RoomType>("Bedroom");
  const [newName, setNewName] = useState("");
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editType, setEditType] = useState<RoomType>("Bedroom");
  const rooms = property.rooms;

  const move = (index: number, delta: number) => run(async () => {
    const ids = rooms.map((r) => r.id);
    const [item] = ids.splice(index, 1);
    ids.splice(index + delta, 0, item);
    onChanged(await put<Property>(`/api/properties/${property.id}/rooms/order`, { roomIds: ids }));
  });

  return (
    <Card title={t("rooms", { count: rooms.length })}>
      <Notice>{t("roomsNotice")}</Notice>
      <ul className="mb-4 divide-y divide-slate-100">
        {rooms.map((r, i) => (
          <li key={r.id} className="flex flex-wrap items-center gap-2 py-2">
            {editing === r.id ? (
              <form className="flex w-full flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
                onChanged(await put<Property>(`/api/properties/${property.id}/rooms/${r.id}`, { roomType: editType, name: editName }));
                setEditing(null);
              }); }}>
                <Select className="sm:w-40" value={editType} onChange={(e) => setEditType(e.target.value as RoomType)}>{ROOM_TYPES.map((rt) => <option key={rt} value={rt}>{humanize(rt)}</option>)}</Select>
                <Input className="min-w-0 flex-1" value={editName} onChange={(e) => setEditName(e.target.value)} required />
                <Button type="submit">{t("save")}</Button><Button type="button" variant="secondary" onClick={() => setEditing(null)}>{t("cancel")}</Button>
              </form>
            ) : (
              <>
                <span className="w-6 text-sm text-slate-400">{i + 1}</span>
                <span className="flex-1 text-sm"><span className="font-medium">{r.name}</span> <span className="text-slate-500">· {humanize(r.roomType)}</span></span>
                <Button variant="ghost" disabled={i === 0} onClick={() => move(i, -1)} aria-label={t("moveUp")}>↑</Button>
                <Button variant="ghost" disabled={i === rooms.length - 1} onClick={() => move(i, 1)} aria-label={t("moveDown")}>↓</Button>
                <Button variant="ghost" onClick={() => { setEditing(r.id); setEditName(r.name); setEditType(r.roomType); }}>{t("rename")}</Button>
                <Button variant="ghost" onClick={() => { if (confirm(t("confirmRemoveRoom", { name: r.name }))) void run(async () => onChanged(await del<Property>(`/api/properties/${property.id}/rooms/${r.id}`))); }}>{t("remove")}</Button>
              </>
            )}
          </li>
        ))}
      </ul>
      <form className="flex flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
        onChanged(await post<Property>(`/api/properties/${property.id}/rooms`, { roomType: newType, name: newName }));
        setNewName("");
      }); }}>
        <Select className="sm:w-40" value={newType} onChange={(e) => setNewType(e.target.value as RoomType)}>{ROOM_TYPES.map((rt) => <option key={rt} value={rt}>{humanize(rt)}</option>)}</Select>
        <Input className="min-w-0 flex-1" placeholder={t("roomNamePlaceholder")} value={newName} onChange={(e) => setNewName(e.target.value)} required />
        <Button type="submit">{t("addRoom")}</Button>
      </form>
    </Card>
  );
}

function TenanciesCard({ propertyId, tenancies, reload, run }: { propertyId: string; tenancies: Tenancy[]; reload: () => Promise<void>; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { formatDate } = useFormatters();
  const [creating, setCreating] = useState(tenancies.length === 0);
  const [form, setForm] = useState({ startDate: new Date().toISOString().slice(0, 10), endDate: "", reference: "" });
  const [inviteFor, setInviteFor] = useState<string | null>(null);
  const [invite, setInvite] = useState({ email: "", fullName: "" });
  const [lastInvite, setLastInvite] = useState<TenantInvitation | null>(null);

  return (
    <Card title={t("tenancies")} actions={!creating && <Button variant="ghost" onClick={() => setCreating(true)}>{t("newTenancy")}</Button>}>
      {creating && (
        <form className="mb-4 space-y-3 rounded-lg bg-slate-50 p-3" onSubmit={(e) => { e.preventDefault(); void run(async () => {
          await post("/api/tenancies", { propertyId, startDate: form.startDate, endDate: form.endDate || null, reference: form.reference || null });
          setCreating(false);
          await reload();
        }); }}>
          <div className="grid grid-cols-2 gap-3">
            <Field label={t("startDate")}><Input type="date" required value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></Field>
            <Field label={t("endDate")}><Input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></Field>
          </div>
          <Field label={t("reference")}><Input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} /></Field>
          <div className="flex gap-2"><Button type="submit">{t("createTenancy")}</Button>{tenancies.length > 0 && <Button type="button" variant="secondary" onClick={() => setCreating(false)}>{t("cancel")}</Button>}</div>
        </form>
      )}
      {lastInvite && (
        <div className="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 p-3">
          <p className="mb-2 text-sm text-emerald-900">{t("invitationCreated", { name: lastInvite.member.fullName })}</p>
          <CopyField label={t("tenantInvitationLink")} value={lastInvite.invitationLink} />
        </div>
      )}
      {tenancies.length === 0 && !creating && <EmptyState title={t("noTenancies")} />}
      <ul className="space-y-3">
        {tenancies.map((tenancy) => (
          <li key={tenancy.id} className="rounded-lg border border-slate-200 p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-sm font-medium">{formatDate(tenancy.startDate)} – {tenancy.endDate ? formatDate(tenancy.endDate) : t("ongoing")}{tenancy.reference && <span className="text-slate-500"> · {tenancy.reference}</span>}</span>
              <Badge value={tenancy.status} />
            </div>
            <ul className="mt-2 space-y-1 text-sm">
              {tenancy.members.map((m) => (
                <li key={m.id} className="flex justify-between gap-2"><span>{m.fullName} <span className="text-slate-500">{m.email}</span></span>
                  <span className={m.joined ? "text-emerald-700" : "text-amber-700"}>{m.joined ? t("joined") : t("invited")}</span></li>
              ))}
              {tenancy.members.length === 0 && <li className="text-slate-500">{t("noTenants")}</li>}
            </ul>
            {inviteFor === tenancy.id ? (
              <form className="mt-3 flex flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
                setLastInvite(await post<TenantInvitation>(`/api/tenancies/${tenancy.id}/tenants`, invite));
                setInvite({ email: "", fullName: "" });
                setInviteFor(null);
                await reload();
              }); }}>
                <Input className="min-w-0 flex-1" placeholder={t("tenantName")} required value={invite.fullName} onChange={(e) => setInvite({ ...invite, fullName: e.target.value })} />
                <Input className="min-w-0 flex-1" type="email" placeholder={t("tenantEmailPlaceholder")} required value={invite.email} onChange={(e) => setInvite({ ...invite, email: e.target.value })} />
                <Button type="submit">{t("invite")}</Button>
              </form>
            ) : (
              <Button variant="ghost" className="mt-2" onClick={() => setInviteFor(tenancy.id)}>{t("inviteTenant")}</Button>
            )}
          </li>
        ))}
      </ul>
    </Card>
  );
}
