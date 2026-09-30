"use client";

import { useEffect, useState } from "react";
import { AiTextEditor, aiTextState } from "@/components/inspection/AiTextEditor";
import { SaveStatus, useReportSaveStatus } from "@/components/inspection/SaveStatus";
import { Icon } from "@/components/icons";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { del, get, put } from "@/lib/api";
import { useAutosave } from "@/lib/autosave";
import { cx } from "@/lib/cx";
import type { Defect, DefectClassification, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { PhotoUploader } from "./PhotoUploader";
import { Field, IconButton, Input, Select, StatusBadge, Switch, useConfirm } from "./ui";

const CLASSIFICATIONS: DefectClassification[] = ["Unknown", "PreExisting", "NewDamage", "NormalWear", "Unchanged", "Resolved"];

type DefectFields = { description: string; location: string; finalDescription: string; classification: DefectClassification; agentConfirmed: boolean };
const fieldsOf = (d: Defect): DefectFields => ({
  description: d.description ?? "", location: d.location ?? "", finalDescription: d.finalDescription ?? "",
  classification: d.classification, agentConfirmed: d.agentConfirmed,
});

/** Collapsed summary row: number, label, location, classification and confirmation — expands to edit. */
function DefectSummary({ index, fields, open, onToggle, onRemove }: { index: number; fields: DefectFields; open: boolean; onToggle: () => void; onRemove?: () => void }) {
  const t = useT(inspectionToolsMessages);
  return (
    <div className="flex items-center gap-2">
      <button type="button" onClick={onToggle} aria-expanded={open} className="flex min-w-0 flex-1 items-center gap-2.5 py-1 text-left">
        <span className="tabular grid h-7 w-7 shrink-0 place-items-center rounded-md bg-warning-50 text-caption font-semibold text-warning-700">{index + 1}</span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-body font-semibold text-ink">{fields.description || t("defectNumber", { number: index + 1 })}</span>
          {fields.location && <span className="block truncate text-caption text-ink-3">{fields.location}</span>}
        </span>
        {fields.classification !== "Unknown" && <StatusBadge value={fields.classification} className="hidden sm:inline-flex" />}
        {fields.agentConfirmed && <span className="shrink-0 text-success-500"><Icon name="checkCircle" className="h-4.5 w-4.5" /><span className="sr-only">{t("confirmed")}</span></span>}
        <Icon name="chevronDown" className={cx("h-4 w-4 shrink-0 text-ink-3 transition", open && "rotate-180")} />
      </button>
      {onRemove && <IconButton icon="trash" label={t("remove")} onClick={onRemove} className="hover:text-danger-700" />}
    </div>
  );
}

/**
 * One defect on the descriptions step. Starts open until the inspector has confirmed it, so what still needs
 * work is expanded and what is done stays compact (progressive disclosure).
 */
export function DefectEditor({ defect, index, base, editable, moveOut, onRoom, reload, registerFlush }: {
  defect: Defect; index: number; base: string; editable: boolean; moveOut: boolean;
  onRoom: (room: RoomDetail) => void; reload: () => Promise<void>;
  /** Lets a parent save pending edits before acting (returns an unregister function). */
  registerFlush?: (key: string, flush: () => Promise<void>) => () => void;
}) {
  const t = useT(inspectionToolsMessages);
  const { humanize } = useFormatters();
  const [confirm, confirmDialog] = useConfirm();
  const [fields, setFields] = useState<DefectFields>(() => fieldsOf(defect));
  const [open, setOpen] = useState(() => !(defect.agentConfirmed && defect.finalDescription));
  const url = `${base}/defects/${defect.id}`;
  const autosave = useAutosave(fields, async (v) => {
    onRoom(await put<RoomDetail>(url, { ...v, description: v.description || null, location: v.location || null, finalDescription: v.finalDescription || null }));
  }, { enabled: editable });
  const { flush } = autosave;
  useEffect(() => registerFlush?.(`defect:${defect.id}`, flush), [registerFlush, defect.id, flush]);
  useReportSaveStatus(`defect:${defect.id}`, autosave.state, autosave.error);
  const set = <K extends keyof DefectFields>(k: K, v: DefectFields[K]) => setFields((f) => ({ ...f, [k]: v }));

  async function afterAi() {
    await reload();
    // Adopt AI pre-fill only if the agent has not written anything yet.
    if (!fields.finalDescription) {
      const fresh = await get<RoomDetail>(base);
      const d = fresh.defects.find((x) => x.id === defect.id);
      if (d?.finalDescription) { const next = { ...fields, finalDescription: d.finalDescription }; autosave.markSaved(next); setFields(next); }
    }
  }
  async function remove() {
    if (await confirm({ title: t("confirmRemoveDefect"), confirmLabel: t("remove"), danger: true })) onRoom(await del<RoomDetail>(url));
  }

  const writing = defect.latestAnalysis?.status === "Pending" || defect.latestAnalysis?.status === "Processing";
  const state = aiTextState({ value: fields.finalDescription, aiText: defect.aiDescription, writing, reviewed: fields.agentConfirmed });
  return (
    <div data-testid="defect-card" className={cx("rounded-lg border bg-surface px-3 py-2 transition sm:px-4", open ? "border-warning-500/40 shadow-card" : "border-line")}>
      <DefectSummary index={index} fields={fields} open={open} onToggle={() => setOpen((o) => !o)} onRemove={editable ? () => void remove() : undefined} />
      {open && (
        <div className="space-y-4 pb-2 pt-3 animate-fade-in">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label={t("shortLabel")}><Input disabled={!editable} value={fields.description} onChange={(e) => set("description", e.target.value)} placeholder={t("shortLabelPlaceholder")} /></Field>
            <Field label={t("location")}><Input disabled={!editable} value={fields.location} onChange={(e) => set("location", e.target.value)} placeholder={t("locationPlaceholder")} /></Field>
          </div>
          <PhotoUploader compact label={t("defectPhotos")} uploadUrl={`${base}/photos`} deleteUrlBase={base.replace(/\/rooms\/[^/]+$/, "/photos")}
            photos={defect.photos} mediaType="Defect" defectId={defect.id} editable={editable} onChanged={reload} />
          <AiTextEditor label={t("defectDescriptionLabel")} rows={3} disabled={!editable} value={fields.finalDescription} onChange={(v) => set("finalDescription", v)}
            aiText={defect.aiDescription} state={state} confidence={defect.aiConfidence}
            tools={<AiAnalysisButton label={t("describeDefectAi")} requestUrl={`${url}/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
              latest={defect.latestAnalysis} disabled={!editable || defect.photos.length === 0} onDone={afterAi} />} />
          <div className="grid items-end gap-3 sm:grid-cols-2">
            <Field label={t("classification")} hint={moveOut ? t("classificationHintMoveOut") : t("classificationHintMoveIn")}>
              <Select disabled={!editable} value={fields.classification} onChange={(e) => set("classification", e.target.value as DefectClassification)}>
                {CLASSIFICATIONS.map((c) => <option key={c} value={c}>{humanize(c)}</option>)}
              </Select>
            </Field>
            <div className="rounded-md border border-line bg-surface-2 px-3 py-3">
              <Switch label={t("confirmDefect")} description={fields.finalDescription.trim() ? undefined : t("confirmNeedsText")}
                disabled={!editable || !fields.finalDescription.trim()} checked={fields.agentConfirmed} onChange={(v) => set("agentConfirmed", v)} />
            </div>
          </div>
          <SaveStatus value={{ state: autosave.state, error: autosave.error }} />
        </div>
      )}
      {confirmDialog}
    </div>
  );
}
