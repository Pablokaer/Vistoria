"use client";

import { useState } from "react";
import { del, get, put } from "@/lib/api";
import { SaveIndicatorText, useAutosave } from "@/lib/autosave";
import { humanize } from "@/lib/format";
import type { Defect, DefectClassification, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { PhotoUploader } from "./PhotoUploader";
import { Button, Field, Input, Select, Textarea } from "./ui";

const CLASSIFICATIONS: DefectClassification[] = ["Unknown", "PreExisting", "NewDamage", "NormalWear", "Unchanged", "Resolved"];

export function DefectEditor({ defect, index, base, editable, moveOut, onRoom, reload }: {
  defect: Defect; index: number; base: string; editable: boolean; moveOut: boolean;
  onRoom: (room: RoomDetail) => void; reload: () => Promise<void>;
}) {
  const [fields, setFields] = useState({
    description: defect.description ?? "", location: defect.location ?? "", finalDescription: defect.finalDescription ?? "",
    classification: defect.classification, agentConfirmed: defect.agentConfirmed,
  });
  const url = `${base}/defects/${defect.id}`;
  const autosave = useAutosave(fields, async (v) => {
    onRoom(await put<RoomDetail>(url, { ...v, description: v.description || null, location: v.location || null, finalDescription: v.finalDescription || null }));
  }, { enabled: editable });
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
        <h4 className="font-medium">Defect #{index + 1}</h4>
        {editable && <Button variant="ghost" onClick={async () => { if (confirm("Remove this defect and its photos?")) onRoom(await del<RoomDetail>(url)); }}>Remove</Button>}
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Short label"><Input disabled={!editable} value={fields.description} onChange={(e) => set("description", e.target.value)} placeholder="e.g. Scuff marks" /></Field>
        <Field label="Location"><Input disabled={!editable} value={fields.location} onChange={(e) => set("location", e.target.value)} placeholder="e.g. Wall left of the door" /></Field>
      </div>
      <div className="mt-3">
        <PhotoUploader label="Defect photos" uploadUrl={`${base}/photos`} deleteUrlBase={base.replace(/\/rooms\/[^/]+$/, "/photos")}
          photos={defect.photos} mediaType="Defect" defectId={defect.id} editable={editable} onChanged={reload} />
      </div>
      <div className="mt-3">
        <AiAnalysisButton label="Describe defect with AI" requestUrl={`${url}/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={defect.latestAnalysis} disabled={!editable || defect.photos.length === 0} onDone={afterAi} />
        {defect.aiDescription && (
          <details className="mt-2 rounded-lg bg-white p-2 text-sm">
            <summary className="cursor-pointer text-slate-600">AI draft{defect.aiConfidence != null && ` (confidence ${Math.round(defect.aiConfidence * 100)}%)`}</summary>
            <p className="mt-1 whitespace-pre-line text-slate-700">{defect.aiDescription}</p>
            {editable && <Button variant="ghost" className="mt-1" onClick={() => set("finalDescription", defect.aiDescription ?? "")}>Use AI text</Button>}
          </details>
        )}
      </div>
      <div className="mt-3">
        <Field label="Defect description (as it will appear in the report)">
          <Textarea rows={3} disabled={!editable} value={fields.finalDescription} onChange={(e) => set("finalDescription", e.target.value)} />
        </Field>
      </div>
      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <Field label="Classification" hint={moveOut ? "Your decision — AI suggestions are advisory only." : "For Move In, usually Pre-existing."}>
          <Select disabled={!editable} value={fields.classification} onChange={(e) => set("classification", e.target.value as DefectClassification)}>
            {CLASSIFICATIONS.map((c) => <option key={c} value={c}>{humanize(c)}</option>)}
          </Select>
        </Field>
        <label className="flex min-h-12 items-center gap-3 self-end rounded-lg border border-slate-200 bg-white px-3">
          <input type="checkbox" className="h-5 w-5 accent-[#0f4c5c]" disabled={!editable || !fields.finalDescription.trim()} checked={fields.agentConfirmed}
            onChange={(e) => set("agentConfirmed", e.target.checked)} />
          <span className="text-sm font-medium">I confirm this defect</span>
        </label>
      </div>
      <p className="mt-2 text-xs text-slate-500">{SaveIndicatorText(autosave.state, autosave.error)}</p>
    </div>
  );
}
