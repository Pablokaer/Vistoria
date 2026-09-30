"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Button, Card, CopyField, ErrorBanner, Field, Input, LinkButton, Notice, PageHeader, Select, Textarea } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { formatDate, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { InspectionSummary, InspectionType, PropertySummary, PublishResult, Tenancy, Visibility } from "@/lib/types";

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
  const [visibility, setVisibility] = useState<Visibility>("Public");
  const [comparisonChoice, setComparisonId] = useState<string | null>(null);
  const [instructions, setInstructions] = useState("");
  const [scheduledDate, setScheduledDate] = useState("");
  const [acceptBy, setAcceptBy] = useState("");
  const [inviteEmail, setInviteEmail] = useState("");
  const [publishNow, setPublishNow] = useState(true);
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
        propertyId, tenancyId: tenancyId || null, inspectionType: type, visibility, comparisonInspectionId: comparisonId || null,
        instructions: instructions || null, scheduledDate: scheduledDate || null,
        acceptBy: acceptBy ? new Date(acceptBy).toISOString() : null, publishNow, inviteEmail: inviteEmail || null,
      }));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (result) {
    const i = result.inspection;
    return (
      <div className="mx-auto max-w-2xl">
        <PageHeader title={i.status === "Draft" ? "Draft saved" : "Inspection published"} subtitle={`${humanize(i.inspectionType)} · ${i.property.addressLine1}`} />
        <Card>
          {result.invitation ? (
            <div className="space-y-4">
              <Notice tone="warning">Copy the link and the access code now — for security they are stored only as hashes and cannot be shown again (you can generate a new pair later).
                Share the code through a different channel than the link.</Notice>
              <CopyField label="Invitation link" value={result.invitation.link} />
              <div>
                <span className="mb-1 block text-sm font-medium text-slate-700">Access code</span>
                <div className="rounded-lg bg-slate-900 px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{result.invitation.accessCode}</div>
                <p className="mt-1 text-xs text-slate-500">Valid until {formatDate(result.invitation.expiresAt)} · {result.invitation.maxAttempts} attempts</p>
              </div>
            </div>
          ) : (
            <p className="text-sm text-slate-700">{i.status === "Open" ? `Agents can now find this inspection in the marketplace. ${i.rooms.length} rooms were captured from the property.` : "You can publish it from the inspection page."}</p>
          )}
          <div className="mt-5"><LinkButton href={`/company/inspections/${i.id}`}>Go to inspection</LinkButton></div>
        </Card>
      </div>
    );
  }

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
          <Field label="Inspection type">
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
              {TYPES.map((t) => (
                <button type="button" key={t} onClick={() => setType(t)}
                  className={`rounded-lg border px-3 py-2 text-sm font-medium ${type === t ? "border-brand bg-brand-50 text-brand" : "border-slate-300 bg-white"}`}>{humanize(t)}</button>
              ))}
            </div>
          </Field>
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
          <Field label="Visibility">
            <div className="grid gap-2 sm:grid-cols-2">
              {(["Public", "Private"] as Visibility[]).map((v) => (
                <label key={v} className={`flex cursor-pointer gap-3 rounded-lg border p-3 ${visibility === v ? "border-brand bg-brand-50" : "border-slate-200"}`}>
                  <input type="radio" className="mt-1 accent-[#0f4c5c]" checked={visibility === v} onChange={() => setVisibility(v)} />
                  <span className="text-sm"><span className="block font-medium">{v}</span>
                    <span className="text-slate-600">{v === "Public" ? "Listed for all agents in the marketplace." : "Only an agent with the link and the 6-digit code."}</span></span>
                </label>
              ))}
            </div>
          </Field>
          {visibility === "Private" && (
            <Field label="Agent email (optional)" hint="We notify the agent with the link. The code is shown to you to share separately.">
              <Input type="email" value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} />
            </Field>
          )}
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="Scheduled date (optional)"><Input type="date" value={scheduledDate} onChange={(e) => setScheduledDate(e.target.value)} /></Field>
            <Field label="Accept by (optional)" hint="Expires if nobody accepts it in time."><Input type="datetime-local" value={acceptBy} onChange={(e) => setAcceptBy(e.target.value)} /></Field>
          </div>
          <Field label="Instructions for the agent (optional)" hint="Visible to the agent after accepting.">
            <Textarea rows={3} value={instructions} onChange={(e) => setInstructions(e.target.value)} placeholder="Access, keys, parking…" />
          </Field>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" className="h-4 w-4 accent-[#0f4c5c]" checked={publishNow} onChange={(e) => setPublishNow(e.target.checked)} /> Publish now</label>
          <Button type="submit" size="lg" loading={busy} disabled={!propertyId || tenancies.loading || candidates.loading}>{publishNow ? "Create and publish" : "Save draft"}</Button>
        </form>
      </Card>
    </div>
  );
}

export default function NewInspectionPage() {
  return <Suspense><NewInspectionForm /></Suspense>;
}
