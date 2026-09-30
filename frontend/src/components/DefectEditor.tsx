"use client";

import { useEffect, useState } from "react";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { autosaveMessages, inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { del, get, put } from "@/lib/api";
import { SaveIndicatorText, useAutosave } from "@/lib/autosave";
import type { Defect, DefectClassification, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { PhotoUploader } from "./PhotoUploader";
import { Button, Field, Input, Select, Textarea } from "./ui";

const CLASSIFICATIONS: DefectClassification[] = ["Unknown", "PreExisting", "NewDamage", "NormalWear", "Unchanged", "Resolved"];

export function DefectEditor({ defect, index, base, editable, moveOut, onRoom, reload, registerFlush }: {
  defect: Defect; index: number; base: string; editable: boolean; moveOut: boolean;
  onRoom: (room: RoomDetail) => void; reload: () => Promise<void>;
  /** Lets a parent save pending edits before acting (returns an unregister function). */
  registerFlush?: (key: string, flush: () => Promise<void>) => () => void;
}) {
  const t = useT(inspectionToolsMessages);
  const tSave = useT(autosaveMessages);
  const { humanize } = useFormatters();
  const [fields, setFields] = useState({
    description: defect.description ?? "", location: defect.location ?? "", finalDescription: defect.finalDescription ?? "",
    classification: defect.classification, agentConfirmed: defect.agentConfirmed,
  });
  const url = `${base}/defects/${defect.id}`;
  const autosave = useAutosave(fields, async (v) => {
    onRoom(await put<RoomDetail>(url, { ...v, description: v.description || null, location: v.location || null, finalDescription: v.finalDescription || null }));
  }, { enabled: editable });
  const { flush } = autosave;
  useEffect(() => registerFlush?.(`defect:${defect.id}`, flush), [registerFlush, defect.id, flush]);
  const set = <K extends keyof typeof fields>(k: K, v: (typeof fields)[K]) => setFields((f) => ({ ...f, [k]: v }));

  async function afterAi() {
    await reload();
    // Adopt AI pre-fill only if the agent has not written anything yet.
    if (!fields.finalDescription) {
      const fresh = await get<RoomDetail>(base);
      const d = fresh.defects.find((x) => x.id === defect.id);
      if (d?.finalDescription) { const next = { ...fields, finalDescription: d.finalDescription }; autosave.markSaved(next); setFields(next); }
    }
  }

  return (
    <div data-testid="defect-card" className="rounded-xl border border-amber-200 bg-amber-50/40 p-3 sm:p-4">
      <div className="mb-3 flex items-center justify-between">
        <h4 className="font-medium">{t("defectNumber", { number: index + 1 })}</h4>
        {editable && <Button variant="ghost" onClick={async () => { if (confirm(t("confirmRemoveDefect"))) onRoom(await del<RoomDetail>(url)); }}>{t("remove")}</Button>}
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label={t("shortLabel")}><Input disabled={!editable} value={fields.description} onChange={(e) => set("description", e.target.value)} placeholder={t("shortLabelPlaceholder")} /></Field>
        <Field label={t("location")}><Input disabled={!editable} value={fields.location} onChange={(e) => set("location", e.target.value)} placeholder={t("locationPlaceholder")} /></Field>
      </div>
      <div className="mt-3">
        <PhotoUploader label={t("defectPhotos")} uploadUrl={`${base}/photos`} deleteUrlBase={base.replace(/\/rooms\/[^/]+$/, "/photos")}
          photos={defect.photos} mediaType="Defect" defectId={defect.id} editable={editable} onChanged={reload} />
      </div>
      <div className="mt-3">
        <AiAnalysisButton label={t("describeDefectAi")} requestUrl={`${url}/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={defect.latestAnalysis} disabled={!editable || defect.photos.length === 0} onDone={afterAi} />
        {defect.aiDescription && (
          <details className="mt-2 rounded-lg bg-white p-2 text-sm">
            <summary className="cursor-pointer text-slate-600">{t("aiDraft")}{defect.aiConfidence != null && t("aiConfidence", { percent: Math.round(defect.aiConfidence * 100) })}</summary>
            <p className="mt-1 whitespace-pre-line text-slate-700">{defect.aiDescription}</p>
            {editable && <Button variant="ghost" className="mt-1" onClick={() => set("finalDescription", defect.aiDescription ?? "")}>{t("useAiText")}</Button>}
          </details>
        )}
      </div>
      <div className="mt-3">
        <Field label={t("defectDescriptionLabel")}>
          <Textarea rows={3} disabled={!editable} value={fields.finalDescription} onChange={(e) => set("finalDescription", e.target.value)} />
        </Field>
      </div>
      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <Field label={t("classification")} hint={moveOut ? t("classificationHintMoveOut") : t("classificationHintMoveIn")}>
          <Select disabled={!editable} value={fields.classification} onChange={(e) => set("classification", e.target.value as DefectClassification)}>
            {CLASSIFICATIONS.map((c) => <option key={c} value={c}>{humanize(c)}</option>)}
          </Select>
        </Field>
        <label className="flex min-h-12 items-center gap-3 self-end rounded-lg border border-slate-200 bg-white px-3">
          <input type="checkbox" className="h-5 w-5 accent-brand" disabled={!editable || !fields.finalDescription.trim()} checked={fields.agentConfirmed}
            onChange={(e) => set("agentConfirmed", e.target.checked)} />
          <span className="text-sm font-medium">{t("confirmDefect")}</span>
        </label>
      </div>
      <p className="mt-2 text-xs text-slate-500">{SaveIndicatorText(autosave.state, autosave.error, tSave)}</p>
    </div>
  );
}
