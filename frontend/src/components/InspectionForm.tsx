"use client";

import { Icon, type IconName } from "@/components/icons";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
import { cx } from "@/lib/cx";
import type { InspectionType, Visibility } from "@/lib/types";
import { Field, Input, Switch, Textarea } from "./ui";

export { InspectionCreated } from "./company/InspectionCreated";

/** Inspection fields that do not depend on the property (shared by "New inspection" and "Add property"). */
export interface InspectionSettings {
  visibility: Visibility; inviteEmail: string; scheduledDate: string; acceptBy: string; instructions: string; publishNow: boolean;
}

export const DEFAULT_SETTINGS: InspectionSettings = { visibility: "Public", inviteEmail: "", scheduledDate: "", acceptBy: "", instructions: "", publishNow: true };

export const settingsPayload = (s: InspectionSettings) => ({
  visibility: s.visibility, instructions: s.instructions || null, scheduledDate: s.scheduledDate || null,
  acceptBy: s.acceptBy ? new Date(s.acceptBy).toISOString() : null, publishNow: s.publishNow, inviteEmail: s.inviteEmail || null,
});

// Same identity as InspectionTypeTag: teal door-in, orange door-out, indigo calendar.
const TYPE_STYLE: Record<InspectionType, { icon: IconName; active: string; hint: "typeHintMoveIn" | "typeHintMoveOut" | "typeHintPeriodic" | "typeHintOther" }> = {
  MoveIn: { icon: "doorIn", active: "border-movein bg-movein-50 text-movein", hint: "typeHintMoveIn" },
  MoveOut: { icon: "doorOut", active: "border-moveout bg-moveout-50 text-moveout", hint: "typeHintMoveOut" },
  Periodic: { icon: "calendar", active: "border-periodic bg-periodic-50 text-periodic", hint: "typeHintPeriodic" },
  Other: { icon: "clipboard", active: "border-neutral-700 bg-neutral-50 text-neutral-700", hint: "typeHintOther" },
};

/** Segmented type choice; the button's accessible name is only the type label (the e2e clicks "Move Out"). */
export function InspectionTypePicker({ types, value, onChange }: { types: InspectionType[]; value: InspectionType; onChange: (t: InspectionType) => void }) {
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  return (
    <div role="group" aria-label={t("inspectionType")}>
      <div className={cx("grid grid-cols-2 gap-2", types.length > 3 ? "sm:grid-cols-4" : "sm:grid-cols-3")}>
        {types.map((type) => (
          <button type="button" key={type} onClick={() => onChange(type)} aria-pressed={value === type}
            className={cx("flex min-h-11 items-center gap-2 rounded-md border px-3 py-2 text-body font-medium transition",
              value === type ? TYPE_STYLE[type].active : "border-line-strong bg-surface text-ink-2 hover:border-ink-4")}>
            <Icon name={TYPE_STYLE[type].icon} className="h-5 w-5 shrink-0" />{humanize(type)}
          </button>
        ))}
      </div>
      <p className="mt-2 text-label text-ink-3">{t(TYPE_STYLE[value].hint)}</p>
    </div>
  );
}

function VisibilityOption({ v, selected, onSelect }: { v: Visibility; selected: boolean; onSelect: () => void }) {
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  return (
    <label className={cx("flex cursor-pointer gap-3 rounded-md border p-3 transition", selected ? "border-brand bg-brand-50/60 ring-1 ring-brand/20" : "border-line-strong hover:border-ink-4")}>
      <input type="radio" name="visibility" className="mt-1 accent-brand" checked={selected} onChange={onSelect} />
      <Icon name={v === "Public" ? "globe" : "lock"} className={cx("mt-0.5 h-5 w-5 shrink-0", selected ? "text-brand" : "text-ink-3")} />
      <span className="text-body"><span className="block font-medium text-ink">{humanize(v)}</span>
        <span className="text-label text-ink-3">{v === "Public" ? t("visibilityPublicHint") : t("visibilityPrivateHint")}</span></span>
    </label>
  );
}

/** Marketplace vs private invitation (+ optional inspector email). */
export function InspectionAssignmentFields({ value, onChange }: { value: InspectionSettings; onChange: (s: InspectionSettings) => void }) {
  const t = useT(companyFormMessages);
  return (
    <div className="space-y-4">
      <fieldset>
        <legend className="mb-1.5 text-label font-medium text-ink-2">{t("visibility")}</legend>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {(["Public", "Private"] as Visibility[]).map((v) => (
            <VisibilityOption key={v} v={v} selected={value.visibility === v} onSelect={() => onChange({ ...value, visibility: v })} />
          ))}
        </div>
      </fieldset>
      {value.visibility === "Private" && (
        <Field label={t("agentEmail")} hint={t("agentEmailHint")}>
          <Input type="email" value={value.inviteEmail} onChange={(e) => onChange({ ...value, inviteEmail: e.target.value })} />
        </Field>
      )}
    </div>
  );
}

/** Dates, instructions and the publish switch. */
export function InspectionScheduleFields({ value, onChange }: { value: InspectionSettings; onChange: (s: InspectionSettings) => void }) {
  const t = useT(companyFormMessages);
  const set = <K extends keyof InspectionSettings>(k: K, v: InspectionSettings[K]) => onChange({ ...value, [k]: v });
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label={t("scheduledDate")}><Input type="date" value={value.scheduledDate} onChange={(e) => set("scheduledDate", e.target.value)} /></Field>
        <Field label={t("acceptBy")} hint={t("acceptByHint")}><Input type="datetime-local" value={value.acceptBy} onChange={(e) => set("acceptBy", e.target.value)} /></Field>
      </div>
      <Field label={t("instructions")} hint={t("instructionsHint")}>
        <Textarea rows={3} value={value.instructions} onChange={(e) => set("instructions", e.target.value)} placeholder={t("instructionsPlaceholder")} />
      </Field>
      <div className="border-t border-line pt-4">
        <Switch checked={value.publishNow} onChange={(v) => set("publishNow", v)} label={t("publishNow")} description={t("publishNowHint")} />
      </div>
    </div>
  );
}
