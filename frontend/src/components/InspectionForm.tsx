"use client";

import { formatDate, humanize } from "@/lib/format";
import type { InspectionType, PublishResult, Visibility } from "@/lib/types";
import { Card, CopyField, Field, Input, LinkButton, Notice, PageHeader, Textarea } from "./ui";

/** Inspection fields that do not depend on the property (shared by "New inspection" and "Add property"). */
export interface InspectionSettings {
  visibility: Visibility; inviteEmail: string; scheduledDate: string; acceptBy: string; instructions: string; publishNow: boolean;
}

export const DEFAULT_SETTINGS: InspectionSettings = { visibility: "Public", inviteEmail: "", scheduledDate: "", acceptBy: "", instructions: "", publishNow: true };

export const settingsPayload = (s: InspectionSettings) => ({
  visibility: s.visibility, instructions: s.instructions || null, scheduledDate: s.scheduledDate || null,
  acceptBy: s.acceptBy ? new Date(s.acceptBy).toISOString() : null, publishNow: s.publishNow, inviteEmail: s.inviteEmail || null,
});

export function InspectionTypePicker({ types, value, onChange }: { types: InspectionType[]; value: InspectionType; onChange: (t: InspectionType) => void }) {
  return (
    <Field label="Inspection type">
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        {types.map((t) => (
          <button type="button" key={t} onClick={() => onChange(t)}
            className={`rounded-lg border px-3 py-2 text-sm font-medium disabled:opacity-60 ${value === t ? "border-brand bg-brand-50 text-brand" : "border-slate-300 bg-white"}`}>{humanize(t)}</button>
        ))}
      </div>
    </Field>
  );
}

export function InspectionSettingsFields({ value, onChange }: { value: InspectionSettings; onChange: (s: InspectionSettings) => void }) {
  const set = <K extends keyof InspectionSettings>(k: K, v: InspectionSettings[K]) => onChange({ ...value, [k]: v });
  return (
    <>
      <Field label="Visibility">
        <div className="grid gap-2 sm:grid-cols-2">
          {(["Public", "Private"] as Visibility[]).map((v) => (
            <label key={v} className={`flex cursor-pointer gap-3 rounded-lg border p-3 ${value.visibility === v ? "border-brand bg-brand-50" : "border-slate-200"}`}>
              <input type="radio" className="mt-1 accent-brand" checked={value.visibility === v} onChange={() => set("visibility", v)} />
              <span className="text-sm"><span className="block font-medium">{v}</span>
                <span className="text-slate-600">{v === "Public" ? "Listed for all agents in the marketplace." : "Only an agent with the link and the 6-digit code."}</span></span>
            </label>
          ))}
        </div>
      </Field>
      {value.visibility === "Private" && (
        <Field label="Agent email (optional)" hint="We notify the agent with the link. The code is shown to you to share separately.">
          <Input type="email" value={value.inviteEmail} onChange={(e) => set("inviteEmail", e.target.value)} />
        </Field>
      )}
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Scheduled date (optional)"><Input type="date" value={value.scheduledDate} onChange={(e) => set("scheduledDate", e.target.value)} /></Field>
        <Field label="Accept by (optional)" hint="Expires if nobody accepts it in time."><Input type="datetime-local" value={value.acceptBy} onChange={(e) => set("acceptBy", e.target.value)} /></Field>
      </div>
      <Field label="Instructions for the agent (optional)" hint="Visible to the agent after accepting.">
        <Textarea rows={3} value={value.instructions} onChange={(e) => set("instructions", e.target.value)} placeholder="Access, keys, parking…" />
      </Field>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" className="h-4 w-4 accent-brand" checked={value.publishNow} onChange={(e) => set("publishNow", e.target.checked)} /> Publish now
      </label>
    </>
  );
}

/** Shown once after creating an inspection (a private invitation's code can never be displayed again). */
export function InspectionCreated({ result, extra }: { result: PublishResult; extra?: React.ReactNode }) {
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
        <div className="mt-5 flex flex-wrap gap-2"><LinkButton href={`/company/inspections/${i.id}`}>Go to inspection</LinkButton>{extra}</div>
      </Card>
    </div>
  );
}
