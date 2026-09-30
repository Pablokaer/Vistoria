"use client";

import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
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
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  return (
    <Field label={t("inspectionType")}>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        {types.map((type) => (
          <button type="button" key={type} onClick={() => onChange(type)}
            className={`rounded-lg border px-3 py-2 text-sm font-medium disabled:opacity-60 ${value === type ? "border-brand bg-brand-50 text-brand" : "border-slate-300 bg-white"}`}>{humanize(type)}</button>
        ))}
      </div>
    </Field>
  );
}

function VisibilityField({ value, onChange }: { value: Visibility; onChange: (v: Visibility) => void }) {
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  return (
    <Field label={t("visibility")}>
      <div className="grid gap-2 sm:grid-cols-2">
        {(["Public", "Private"] as Visibility[]).map((v) => (
          <label key={v} className={`flex cursor-pointer gap-3 rounded-lg border p-3 ${value === v ? "border-brand bg-brand-50" : "border-slate-200"}`}>
            <input type="radio" className="mt-1 accent-brand" checked={value === v} onChange={() => onChange(v)} />
            <span className="text-sm"><span className="block font-medium">{humanize(v)}</span>
              <span className="text-slate-600">{v === "Public" ? t("visibilityPublicHint") : t("visibilityPrivateHint")}</span></span>
          </label>
        ))}
      </div>
    </Field>
  );
}

export function InspectionSettingsFields({ value, onChange }: { value: InspectionSettings; onChange: (s: InspectionSettings) => void }) {
  const t = useT(companyFormMessages);
  const set = <K extends keyof InspectionSettings>(k: K, v: InspectionSettings[K]) => onChange({ ...value, [k]: v });
  return (
    <>
      <VisibilityField value={value.visibility} onChange={(v) => set("visibility", v)} />
      {value.visibility === "Private" && (
        <Field label={t("agentEmail")} hint={t("agentEmailHint")}>
          <Input type="email" value={value.inviteEmail} onChange={(e) => set("inviteEmail", e.target.value)} />
        </Field>
      )}
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={t("scheduledDate")}><Input type="date" value={value.scheduledDate} onChange={(e) => set("scheduledDate", e.target.value)} /></Field>
        <Field label={t("acceptBy")} hint={t("acceptByHint")}><Input type="datetime-local" value={value.acceptBy} onChange={(e) => set("acceptBy", e.target.value)} /></Field>
      </div>
      <Field label={t("instructions")} hint={t("instructionsHint")}>
        <Textarea rows={3} value={value.instructions} onChange={(e) => set("instructions", e.target.value)} placeholder={t("instructionsPlaceholder")} />
      </Field>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" className="h-4 w-4 accent-brand" checked={value.publishNow} onChange={(e) => set("publishNow", e.target.checked)} /> {t("publishNow")}
      </label>
    </>
  );
}

function InvitationDetails({ invitation }: { invitation: NonNullable<PublishResult["invitation"]> }) {
  const t = useT(companyFormMessages);
  const { formatDate } = useFormatters();
  return (
    <div className="space-y-4">
      <Notice tone="warning">{t("copyNowWarning")}</Notice>
      <CopyField label={t("invitationLink")} value={invitation.link} />
      <div>
        <span className="mb-1 block text-sm font-medium text-slate-700">{t("accessCode")}</span>
        <div className="rounded-lg bg-slate-900 px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{invitation.accessCode}</div>
        <p className="mt-1 text-xs text-slate-500">{t("codeValidity", { date: formatDate(invitation.expiresAt), attempts: invitation.maxAttempts })}</p>
      </div>
    </div>
  );
}

/** Shown once after creating an inspection (a private invitation's code can never be displayed again). */
export function InspectionCreated({ result, extra }: { result: PublishResult; extra?: React.ReactNode }) {
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  const i = result.inspection;
  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader title={i.status === "Draft" ? t("draftSaved") : t("inspectionPublished")} subtitle={`${humanize(i.inspectionType)} · ${i.property.addressLine1}`} />
      <Card>
        {result.invitation ? <InvitationDetails invitation={result.invitation} /> : (
          <p className="text-sm text-slate-700">{i.status === "Open" ? t("openInMarketplace", { rooms: i.rooms.length }) : t("publishFromInspectionPage")}</p>
        )}
        <div className="mt-5 flex flex-wrap gap-2"><LinkButton href={`/company/inspections/${i.id}`}>{t("goToInspection")}</LinkButton>{extra}</div>
      </Card>
    </div>
  );
}
