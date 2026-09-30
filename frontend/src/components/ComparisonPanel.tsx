"use client";

import { useState } from "react";
import { errorMessage, post, put } from "@/lib/api";
import { formatDate, humanize } from "@/lib/format";
import type { ComparisonDecision, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { PhotoStrip } from "./PhotoUploader";
import { Button, Card, Field, Textarea } from "./ui";

const DECISIONS: ComparisonDecision[] = ["Unchanged", "NormalWear", "NewDamage", "PreExisting", "Resolved", "UnableToDetermine"];

export function BaselinePanel({ room }: { room: RoomDetail }) {
  const b = room.comparison?.baseline;
  if (!b) return null;
  return (
    <Card title={`Move In record — ${b.name}`} className="border-violet-200 bg-violet-50/40">
      <p className="mb-2 text-xs text-slate-500">Report {b.reportNumber} · {formatDate(b.completedAt)} · read-only</p>
      <p className="mb-3 whitespace-pre-line text-sm text-slate-800">{b.description ?? "No description recorded."}</p>
      <PhotoStrip urls={b.photoUrls} />
      {b.defects.length > 0 && (
        <div className="mt-3 space-y-2">
          <h4 className="text-sm font-semibold">Defects recorded at Move In</h4>
          {b.defects.map((d, i) => (
            <div key={i} className="rounded-lg bg-white p-2 text-sm">
              <div className="font-medium">{d.title ?? "Defect"} {d.location && <span className="text-slate-500">· {d.location}</span>}</div>
              <p className="text-slate-700">{d.description}</p>
              {d.photoUrls.length > 0 && <div className="mt-2"><PhotoStrip urls={d.photoUrls} /></div>}
            </div>
          ))}
        </div>
      )}
      {b.agentNotes && <p className="mt-3 text-sm text-slate-700"><span className="font-medium">Notes:</span> {b.agentNotes}</p>}
    </Card>
  );
}

export function ComparisonPanel({ room, base, onRoom, reload }: { room: RoomDetail; base: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void> }) {
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
    <Card title="Compare with Move In">
      <p className="mb-3 text-sm text-slate-600">Tools can highlight possible differences. <strong>You</strong> decide what they mean.</p>
      <div className="flex flex-wrap gap-2">
        <Button variant="secondary" disabled={!editable} loading={busy === "basic"} onClick={() => act("basic", async () => onRoom(await post<RoomDetail>(`${base}/comparison/basic`)))}>
          Compare recorded data
        </Button>
        <AiAnalysisButton label="AI visual comparison" requestUrl={`${base}/comparison/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={c.latestAnalysis} disabled={!editable || room.generalPhotos.length === 0} onDone={reload} />
      </div>
      {c.basicComparison && <pre className="mt-3 whitespace-pre-wrap rounded-lg bg-slate-50 p-3 font-sans text-sm text-slate-800">{c.basicComparison}</pre>}
      {c.aiAnalysis && (
        <div className="mt-3 rounded-lg bg-amber-50 p-3 text-sm">
          <div className="mb-1 font-medium text-amber-900">AI suggestion (advisory)</div>
          <p className="whitespace-pre-line text-amber-900">{c.aiAnalysis}</p>
        </div>
      )}
      <div className="mt-4">
        <span className="mb-2 block text-sm font-medium text-slate-700">Your decision</span>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {DECISIONS.map((d) => (
            <button key={d} type="button" disabled={!editable} onClick={() => setDecision(d)}
              className={`min-h-12 rounded-lg border px-3 text-sm font-medium ${decision === d ? "border-brand bg-brand text-white" : "border-slate-300 bg-white"}`}>{humanize(d)}</button>
          ))}
        </div>
      </div>
      <div className="mt-3"><Field label="Comparison notes"><Textarea rows={2} disabled={!editable} value={notes} onChange={(e) => setNotes(e.target.value)} /></Field></div>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
      <div className="mt-3 flex items-center gap-3">
        <Button disabled={!editable || !decision} loading={busy === "decide"}
          onClick={() => act("decide", async () => onRoom(await put<RoomDetail>(`${base}/comparison/decision`, { decision, notes: notes || null })))}>Save decision</Button>
        {c.agentDecision && <span className="text-sm text-emerald-700">Saved: {humanize(c.agentDecision)}</span>}
      </div>
    </Card>
  );
}
