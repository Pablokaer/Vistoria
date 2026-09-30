"use client";

import { useState } from "react";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { errorMessage, post, put } from "@/lib/api";
import type { ComparisonDecision, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { PhotoStrip } from "./PhotoUploader";
import { Button, Card, Field, Textarea } from "./ui";

const DECISIONS: ComparisonDecision[] = ["Unchanged", "NormalWear", "NewDamage", "PreExisting", "Resolved", "UnableToDetermine"];

export function BaselinePanel({ room }: { room: RoomDetail }) {
  const t = useT(inspectionToolsMessages);
  const { formatDate } = useFormatters();
  const b = room.comparison?.baseline;
  if (!b) return null;
  return (
    <Card title={t("moveInRecordTitle", { room: b.name })} className="border-violet-200 bg-violet-50/40">
      <p className="mb-2 text-xs text-slate-500">{t("baselineMeta", { number: b.reportNumber ?? "", date: formatDate(b.completedAt) })}</p>
      <p className="mb-3 whitespace-pre-line text-sm text-slate-800">{b.description ?? t("noDescription")}</p>
      <PhotoStrip urls={b.photoUrls} />
      {b.defects.length > 0 && (
        <div className="mt-3 space-y-2">
          <h4 className="text-sm font-semibold">{t("moveInDefects")}</h4>
          {b.defects.map((d, i) => (
            <div key={i} className="rounded-lg bg-white p-2 text-sm">
              <div className="font-medium">{d.title ?? t("defect")} {d.location && <span className="text-slate-500">· {d.location}</span>}</div>
              <p className="text-slate-700">{d.description}</p>
              {d.photoUrls.length > 0 && <div className="mt-2"><PhotoStrip urls={d.photoUrls} /></div>}
            </div>
          ))}
        </div>
      )}
      {b.agentNotes && <p className="mt-3 text-sm text-slate-700"><span className="font-medium">{t("notesLabel")}</span> {b.agentNotes}</p>}
    </Card>
  );
}

export function ComparisonPanel({ room, base, onRoom, reload }: { room: RoomDetail; base: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void> }) {
  const t = useT(inspectionToolsMessages);
  const { humanize } = useFormatters();
  const c = room.comparison!;
  const [decision, setDecision] = useState<ComparisonDecision | null>(c.agentDecision);
  const [notes, setNotes] = useState(c.agentNotes ?? "");
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const editable = room.editable;

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setError(null);
    try { await fn(); } catch (e) { setError(errorMessage(e)); } finally { setBusy(null); }
  }

  return (
    <Card title={t("compareWithMoveIn")}>
      <p className="mb-3 text-sm text-slate-600">{t("compareIntroLead")}<strong>{t("compareIntroYou")}</strong>{t("compareIntroTail")}</p>
      <div className="flex flex-wrap gap-2">
        <Button variant="secondary" disabled={!editable} loading={busy === "basic"} onClick={() => act("basic", async () => onRoom(await post<RoomDetail>(`${base}/comparison/basic`)))}>
          {t("compareRecorded")}
        </Button>
        <AiAnalysisButton label={t("aiVisualComparison")} requestUrl={`${base}/comparison/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={c.latestAnalysis} disabled={!editable || room.generalPhotos.length === 0} onDone={reload} />
      </div>
      {c.basicComparison && <pre className="mt-3 whitespace-pre-wrap rounded-lg bg-slate-50 p-3 font-sans text-sm text-slate-800">{c.basicComparison}</pre>}
      {c.aiAnalysis && (
        <div className="mt-3 rounded-lg bg-amber-50 p-3 text-sm">
          <div className="mb-1 font-medium text-amber-900">{t("aiSuggestion")}</div>
          <p className="whitespace-pre-line text-amber-900">{c.aiAnalysis}</p>
        </div>
      )}
      <div className="mt-4">
        <span className="mb-2 block text-sm font-medium text-slate-700">{t("yourDecision")}</span>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {DECISIONS.map((d) => (
            <button key={d} type="button" disabled={!editable} onClick={() => setDecision(d)}
              className={`min-h-12 rounded-lg border px-3 text-sm font-medium ${decision === d ? "border-brand bg-brand text-white" : "border-slate-300 bg-white"}`}>{humanize(d)}</button>
          ))}
        </div>
      </div>
      <div className="mt-3"><Field label={t("comparisonNotes")}><Textarea rows={2} disabled={!editable} value={notes} onChange={(e) => setNotes(e.target.value)} /></Field></div>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
      <div className="mt-3 flex items-center gap-3">
        <Button disabled={!editable || !decision} loading={busy === "decide"}
          onClick={() => act("decide", async () => onRoom(await put<RoomDetail>(`${base}/comparison/decision`, { decision, notes: notes || null })))}>{t("saveDecision")}</Button>
        {c.agentDecision && <span className="text-sm text-emerald-700">{t("savedDecision", { decision: humanize(c.agentDecision) })}</span>}
      </div>
    </Card>
  );
}
