"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { Badge, Button, Card, CopyField, EmptyState, ErrorBanner, Field, Input, LinkButton, Loading, Notice, PageHeader, Select } from "@/components/ui";
import { del, errorMessage, post, put } from "@/lib/api";
import { formatDate, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import { PROPERTY_TYPES, ROOM_TYPES, type InspectionSummary, type Property, type RoomType, type Tenancy, type TenantInvitation } from "@/lib/types";

export default function PropertyDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const property = useApi<Property>(`/api/properties/${id}`);
  const tenancies = useApi<Tenancy[]>(`/api/properties/${id}/tenancies`);
  const inspections = useApi<InspectionSummary[]>(`/api/inspections?propertyId=${id}`);
  const [error, setError] = useState<string | null>(null);

  const run = async (fn: () => Promise<unknown>) => {
    setError(null);
    try { await fn(); } catch (e) { setError(errorMessage(e)); }
  };

  if (property.loading && !property.data) return <Loading />;
  if (!property.data) return <ErrorBanner message={property.error ?? "Property not found"} onRetry={property.reload} />;
  const p = property.data;

  return (
    <>
      <PageHeader title={p.addressLine1} subtitle={`${[p.addressLine2, p.city, p.postcode, p.country].filter(Boolean).join(", ")} · ${humanize(p.propertyType)}`}
        back={{ href: "/company/properties", label: "Properties" }}
        actions={<LinkButton href={`/company/inspections/new?propertyId=${p.id}`}>New inspection</LinkButton>} />
      <ErrorBanner message={error} />
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-4">
          <DetailsCard property={p} onSaved={(np) => property.setData(np)} run={run} />
          <RoomsCard property={p} onChanged={(np) => property.setData(np)} run={run} />
        </div>
        <div className="space-y-4">
          <TenanciesCard propertyId={p.id} tenancies={tenancies.data ?? []} reload={tenancies.reload} run={run} />
          <Card title="Inspections" actions={<LinkButton variant="ghost" href={`/company/inspections/new?propertyId=${p.id}`}>+ New</LinkButton>}>
            {!inspections.data?.length ? <EmptyState title="No inspections for this property yet" /> : (
              <ul className="divide-y divide-slate-100">
                {inspections.data.map((i) => (
                  <li key={i.id}>
                    <Link href={`/company/inspections/${i.id}`} className="flex items-center justify-between gap-2 py-2.5 hover:bg-slate-50">
                      <span className="text-sm"><span className="font-medium">{humanize(i.inspectionType)}</span>
                        <span className="text-slate-500"> · {i.roomsCompleted}/{i.roomsTotal} rooms · {i.agentName ?? "unassigned"}</span></span>
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
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({ ...property, addressLine2: property.addressLine2 ?? "" });
  const set = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [k]: e.target.value });
  if (!editing) {
    return (
      <Card title="Property details" actions={<Button variant="ghost" onClick={() => { setForm({ ...property, addressLine2: property.addressLine2 ?? "" }); setEditing(true); }}>Edit</Button>}>
        <p className="text-sm text-slate-700">{[property.addressLine1, property.addressLine2, property.city, property.postcode, property.country].filter(Boolean).join(", ")}</p>
      </Card>
    );
  }
  return (
    <Card title="Edit property">
      <form className="space-y-3" onSubmit={(e) => { e.preventDefault(); void run(async () => {
        onSaved(await put<Property>(`/api/properties/${property.id}`, { ...form, addressLine2: form.addressLine2 || null }));
        setEditing(false);
      }); }}>
        <Field label="Address line 1"><Input required value={form.addressLine1} onChange={set("addressLine1")} /></Field>
        <Field label="Address line 2"><Input value={form.addressLine2} onChange={set("addressLine2")} /></Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="City"><Input required value={form.city} onChange={set("city")} /></Field>
          <Field label="Postcode"><Input required value={form.postcode} onChange={set("postcode")} /></Field>
          <Field label="Country"><Input required value={form.country} onChange={set("country")} /></Field>
          <Field label="Type"><Select value={form.propertyType} onChange={set("propertyType")}>{PROPERTY_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</Select></Field>
        </div>
        <div className="flex gap-2"><Button type="submit">Save</Button><Button type="button" variant="secondary" onClick={() => setEditing(false)}>Cancel</Button></div>
      </form>
    </Card>
  );
}

function RoomsCard({ property, onChanged, run }: { property: Property; onChanged: (p: Property) => void; run: Run }) {
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
    <Card title={`Rooms (${rooms.length})`}>
      <Notice>Changes apply to future inspections only. Published inspections keep the rooms they were created with.</Notice>
      <ul className="mb-4 divide-y divide-slate-100">
        {rooms.map((r, i) => (
          <li key={r.id} className="flex flex-wrap items-center gap-2 py-2">
            {editing === r.id ? (
              <form className="flex w-full flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
                onChanged(await put<Property>(`/api/properties/${property.id}/rooms/${r.id}`, { roomType: editType, name: editName }));
                setEditing(null);
              }); }}>
                <Select className="sm:w-40" value={editType} onChange={(e) => setEditType(e.target.value as RoomType)}>{ROOM_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</Select>
                <Input className="min-w-0 flex-1" value={editName} onChange={(e) => setEditName(e.target.value)} required />
                <Button type="submit">Save</Button><Button type="button" variant="secondary" onClick={() => setEditing(null)}>Cancel</Button>
              </form>
            ) : (
              <>
                <span className="w-6 text-sm text-slate-400">{i + 1}</span>
                <span className="flex-1 text-sm"><span className="font-medium">{r.name}</span> <span className="text-slate-500">· {humanize(r.roomType)}</span></span>
                <Button variant="ghost" disabled={i === 0} onClick={() => move(i, -1)} aria-label="Move up">↑</Button>
                <Button variant="ghost" disabled={i === rooms.length - 1} onClick={() => move(i, 1)} aria-label="Move down">↓</Button>
                <Button variant="ghost" onClick={() => { setEditing(r.id); setEditName(r.name); setEditType(r.roomType); }}>Rename</Button>
                <Button variant="ghost" onClick={() => { if (confirm(`Remove ${r.name}?`)) void run(async () => onChanged(await del<Property>(`/api/properties/${property.id}/rooms/${r.id}`))); }}>Remove</Button>
              </>
            )}
          </li>
        ))}
      </ul>
      <form className="flex flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
        onChanged(await post<Property>(`/api/properties/${property.id}/rooms`, { roomType: newType, name: newName }));
        setNewName("");
      }); }}>
        <Select className="sm:w-40" value={newType} onChange={(e) => setNewType(e.target.value as RoomType)}>{ROOM_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}</Select>
        <Input className="min-w-0 flex-1" placeholder="Room name, e.g. Bedroom 3" value={newName} onChange={(e) => setNewName(e.target.value)} required />
        <Button type="submit">Add room</Button>
      </form>
    </Card>
  );
}

function TenanciesCard({ propertyId, tenancies, reload, run }: { propertyId: string; tenancies: Tenancy[]; reload: () => Promise<void>; run: Run }) {
  const [creating, setCreating] = useState(tenancies.length === 0);
  const [form, setForm] = useState({ startDate: new Date().toISOString().slice(0, 10), endDate: "", reference: "" });
  const [inviteFor, setInviteFor] = useState<string | null>(null);
  const [invite, setInvite] = useState({ email: "", fullName: "" });
  const [lastInvite, setLastInvite] = useState<TenantInvitation | null>(null);

  return (
    <Card title="Tenancies" actions={!creating && <Button variant="ghost" onClick={() => setCreating(true)}>+ New tenancy</Button>}>
      {creating && (
        <form className="mb-4 space-y-3 rounded-lg bg-slate-50 p-3" onSubmit={(e) => { e.preventDefault(); void run(async () => {
          await post("/api/tenancies", { propertyId, startDate: form.startDate, endDate: form.endDate || null, reference: form.reference || null });
          setCreating(false);
          await reload();
        }); }}>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Start date"><Input type="date" required value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></Field>
            <Field label="End date (optional)"><Input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></Field>
          </div>
          <Field label="Reference (optional)"><Input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} /></Field>
          <div className="flex gap-2"><Button type="submit">Create tenancy</Button>{tenancies.length > 0 && <Button type="button" variant="secondary" onClick={() => setCreating(false)}>Cancel</Button>}</div>
        </form>
      )}
      {lastInvite && (
        <div className="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 p-3">
          <p className="mb-2 text-sm text-emerald-900">Invitation created for {lastInvite.member.fullName}. Share this link — it is shown only once (an in-app notification was also queued).</p>
          <CopyField label="Tenant invitation link" value={lastInvite.invitationLink} />
        </div>
      )}
      {tenancies.length === 0 && !creating && <EmptyState title="No tenancies yet" />}
      <ul className="space-y-3">
        {tenancies.map((t) => (
          <li key={t.id} className="rounded-lg border border-slate-200 p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-sm font-medium">{formatDate(t.startDate)} – {t.endDate ? formatDate(t.endDate) : "ongoing"}{t.reference && <span className="text-slate-500"> · {t.reference}</span>}</span>
              <Badge value={t.status} />
            </div>
            <ul className="mt-2 space-y-1 text-sm">
              {t.members.map((m) => (
                <li key={m.id} className="flex justify-between gap-2"><span>{m.fullName} <span className="text-slate-500">{m.email}</span></span>
                  <span className={m.joined ? "text-emerald-700" : "text-amber-700"}>{m.joined ? "Joined" : "Invited"}</span></li>
              ))}
              {t.members.length === 0 && <li className="text-slate-500">No tenants yet.</li>}
            </ul>
            {inviteFor === t.id ? (
              <form className="mt-3 flex flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); void run(async () => {
                setLastInvite(await post<TenantInvitation>(`/api/tenancies/${t.id}/tenants`, invite));
                setInvite({ email: "", fullName: "" });
                setInviteFor(null);
                await reload();
              }); }}>
                <Input className="min-w-0 flex-1" placeholder="Tenant name" required value={invite.fullName} onChange={(e) => setInvite({ ...invite, fullName: e.target.value })} />
                <Input className="min-w-0 flex-1" type="email" placeholder="tenant@email.com" required value={invite.email} onChange={(e) => setInvite({ ...invite, email: e.target.value })} />
                <Button type="submit">Invite</Button>
              </form>
            ) : (
              <Button variant="ghost" className="mt-2" onClick={() => setInviteFor(t.id)}>+ Invite tenant</Button>
            )}
          </li>
        ))}
      </ul>
    </Card>
  );
}
