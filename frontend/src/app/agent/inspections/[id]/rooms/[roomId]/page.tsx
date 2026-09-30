"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { AiAnalysisButton } from "@/components/AiAnalysisButton";
import { BaselinePanel, ComparisonPanel } from "@/components/ComparisonPanel";
import { DefectEditor } from "@/components/DefectEditor";
import { PhotoUploader } from "@/components/PhotoUploader";
import { Badge, Button, Card, ErrorBanner, Field, Loading, Notice, ProgressBar, Textarea } from "@/components/ui";
import { errorMessage, get, post, put } from "@/lib/api";
import { SaveIndicatorText, useAutosave } from "@/lib/autosave";
import { humanize } from "@/lib/format";
import type { RoomDetail } from "@/lib/types";

interface Fields { finalDescription: string; defectsFound: boolean; agentNotes: string }

const draftKey = (roomId: string) => `inspectflow:room-draft:${roomId}`;
const readDraft = (roomId: string): Fields | null => {
  try { const raw = localStorage.getItem(draftKey(roomId)); return raw ? (JSON.parse(raw) as Fields) : null; } catch { return null; }
};
const writeDraft = (roomId: string, f: Fields | null) => {
  try { if (f) localStorage.setItem(draftKey(roomId), JSON.stringify(f)); else localStorage.removeItem(draftKey(roomId)); } catch { /* storage unavailable */ }
};
const fromRoom = (r: RoomDetail): Fields => ({ finalDescription: r.finalDescription ?? "", defectsFound: r.defectsFound, agentNotes: r.agentNotes ?? "" });

export default function RoomPage() {
  const { id, roomId } = useParams<{ id: string; roomId: string }>();
  // Keyed by room so all local state resets when navigating between rooms.
  return <RoomLoader key={roomId} inspectionId={id} roomId={roomId} />;
}

function RoomLoader({ inspectionId, roomId }: { inspectionId: string; roomId: string }) {
  const base = `/api/agent/inspections/${inspectionId}/rooms/${roomId}`;
  const [room, setRoom] = useState<RoomDetail | null>(null);
  const [fields, setFields] = useState<Fields | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const r = await get<RoomDetail>(base);
      setRoom(r);
      setLoadError(null);
      return r;
    } catch (e) { setLoadError(errorMessage(e)); return null; }
  }, [base]);

  useEffect(() => {
    let cancelled = false;
    void get<RoomDetail>(base).then((r) => {
      if (cancelled) return;
      setRoom(r);
      // Restore unsaved text from a previous visit (e.g. page refreshed with no signal).
      const draft = r.editable ? readDraft(roomId) : null;
      setFields(draft ?? fromRoom(r));
    }, (e) => { if (!cancelled) setLoadError(errorMessage(e)); });
    return () => { cancelled = true; };
  }, [base, roomId]);

  if (loadError && !room) return <ErrorBanner message={loadError} onRetry={() => void load()} />;
  if (!room || !fields) return <Loading />;
  return <RoomForm room={room} setRoom={setRoom} initial={fields} base={base} reload={async () => { await load(); }} inspectionId={inspectionId} />;
}

function RoomForm({ room, setRoom, initial, base, reload, inspectionId }: {
  room: RoomDetail; setRoom: (r: RoomDetail) => void; initial: Fields; base: string; reload: () => Promise<void>; inspectionId: string;
}) {
  const router = useRouter();
  const [fields, setFields] = useState<Fields>(initial);
  const [actionError, setActionError] = useState<string | null>(null);
  const [issues, setIssues] = useState<string[]>([]);
  const [busy, setBusy] = useState<string | null>(null);
  const editable = room.editable;

  const autosave = useAutosave(fields, async (v) => {
    const saved = await put<RoomDetail>(base, { finalDescription: v.finalDescription || null, defectsFound: v.defectsFound, agentNotes: v.agentNotes || null });
    setRoom(saved);
    writeDraft(room.id, null);
  }, { enabled: editable, savedValue: fromRoom(room) }); // a restored local draft differs → saved automatically

  useEffect(() => { if (editable && autosave.state === "dirty") writeDraft(room.id, fields); }, [fields, autosave.state, editable, room.id]);

  const set = <K extends keyof Fields>(k: K, v: Fields[K]) => setFields((f) => ({ ...f, [k]: v }));

  async function afterRoomAi() {
    const fresh = await get<RoomDetail>(base);
    setRoom(fresh);
    if (!fields.finalDescription.trim() && fresh.finalDescription) {
      const next = { ...fields, finalDescription: fresh.finalDescription };
      autosave.markSaved(next);
      setFields(next);
    }
  }

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setActionError(null);
    setIssues([]);
    try { await fn(); } catch (e) {
      setActionError(errorMessage(e));
      const details = (e as { details?: string[] }).details;
      if (details?.length) setIssues(details);
    } finally { setBusy(null); }
  }

  const complete = () => act("complete", async () => {
    await autosave.flush();
    const done = await post<RoomDetail>(`${base}/complete`);
    setRoom(done);
    router.push(done.nextRoomId && done.roomsCompleted < done.roomsTotal ? `/agent/inspections/${inspectionId}/rooms/${done.nextRoomId}` : `/agent/inspections/${inspectionId}`);
  });

  const addDefect = () => act("defect", async () => {
    await autosave.flush();
    setRoom(await post<RoomDetail>(`${base}/defects`, { description: null, location: null }));
    set("defectsFound", true);
  });

  const deleteBase = base.replace(/\/rooms\/[^/]+$/, "/photos");
  const analysesBase = base.replace(/\/rooms\/[^/]+$/, "/analyses");
  const hasComparison = !!room.comparison;

  return (
    <div className="pb-28">
      <div className="mb-4">
        <Link href={`/agent/inspections/${inspectionId}`} className="text-sm text-brand hover:underline">← All rooms</Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-2">
          <h1 className="text-2xl font-semibold">{room.name}</h1>
          <Badge value={room.status} />
        </div>
        <p className="mb-3 text-sm text-slate-500">{humanize(room.roomType)} · room {room.sequence} of {room.roomsTotal}</p>
        <ProgressBar value={room.roomsCompleted} total={room.roomsTotal} />
      </div>

      {!editable && <Notice tone="info">This room is read-only ({humanize(room.inspectionStatus)}).</Notice>}
      <ErrorBanner message={actionError} />
      {issues.length > 0 && <ul className="-mt-2 mb-4 list-disc space-y-1 rounded-lg bg-red-50 py-3 pl-8 pr-4 text-sm text-red-800">{issues.map((i) => <li key={i}>{i}</li>)}</ul>}

      <div className="space-y-4">
        {hasComparison && <BaselinePanel room={room} />}

        <Card title="General photos">
          <PhotoUploader uploadUrl={`${base}/photos`} deleteUrlBase={deleteBase} photos={room.generalPhotos} mediaType="General" editable={editable} onChanged={reload} label="Room photos" />
        </Card>

        <Card title="Description">
          <AiAnalysisButton label="Generate AI description" requestUrl={`${base}/analysis`} statusUrlBase={analysesBase} latest={room.latestAnalysis}
            disabled={!editable || room.generalPhotos.length === 0} onDone={afterRoomAi} />
          {room.generalPhotos.length === 0 && <p className="mt-1 text-xs text-slate-500">Add at least one photo to use AI.</p>}
          {room.aiDescription && (
            <details className="mt-3 rounded-lg bg-slate-50 p-3 text-sm" open={room.finalDescriptionSource !== "Agent"}>
              <summary className="cursor-pointer font-medium text-slate-700">AI draft (kept for traceability)</summary>
              <p className="mt-2 whitespace-pre-line text-slate-700">{room.aiDescription}</p>
              {editable && fields.finalDescription !== room.aiDescription && (
                <Button variant="ghost" className="mt-1" onClick={() => set("finalDescription", room.aiDescription ?? "")}>Replace my text with AI draft</Button>
              )}
            </details>
          )}
          <div className="mt-3">
            <Field label="Room description (as it will appear in the report)" hint="Describe only what is visible. Review any AI text before completing the room.">
              <Textarea rows={6} disabled={!editable} value={fields.finalDescription} onChange={(e) => set("finalDescription", e.target.value)} />
            </Field>
          </div>
        </Card>

        <Card title="Defects">
          <label className="flex min-h-12 items-center gap-3 rounded-lg border border-slate-200 px-3">
            <input type="checkbox" className="h-5 w-5 accent-[#0f4c5c]" disabled={!editable || room.defects.length > 0} checked={fields.defectsFound}
              onChange={(e) => set("defectsFound", e.target.checked)} />
            <span className="text-sm font-medium">Defects found</span>
          </label>
          {fields.defectsFound && (
            <div className="mt-3 space-y-3">
              {room.defects.map((d, i) => (
                <DefectEditor key={d.id} defect={d} index={i} base={base} editable={editable} moveOut={hasComparison} onRoom={setRoom} reload={reload} />
              ))}
              {editable && <Button variant="secondary" size="lg" className="w-full" loading={busy === "defect"} onClick={() => void addDefect()}>+ Add defect</Button>}
            </div>
          )}
        </Card>

        {hasComparison && <ComparisonPanel room={room} base={base} onRoom={setRoom} reload={reload} />}

        <Card title="Agent notes">
          <Textarea aria-label="Agent notes" rows={3} disabled={!editable} value={fields.agentNotes} onChange={(e) => set("agentNotes", e.target.value)} placeholder="Meter readings, keys, anything else worth recording…" />
        </Card>

        {room.completionIssues.length > 0 && room.status !== "Completed" && (
          <Card title="Before completing this room">
            <ul className="list-disc space-y-1 pl-5 text-sm text-slate-700">{room.completionIssues.map((i) => <li key={i}>{i}</li>)}</ul>
          </Card>
        )}
      </div>

      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center gap-2 px-4 py-3">
          <span className="hidden flex-1 text-xs text-slate-500 sm:block">{SaveIndicatorText(autosave.state, autosave.error)}</span>
          {room.previousRoomId && <Link href={`/agent/inspections/${inspectionId}/rooms/${room.previousRoomId}`} className="rounded-lg border border-slate-300 px-3 py-3 text-sm">←</Link>}
          {editable && room.status === "Completed" ? (
            <Button size="lg" variant="secondary" className="flex-1 sm:flex-none" loading={busy === "reopen"} onClick={() => act("reopen", async () => setRoom(await post<RoomDetail>(`${base}/reopen`)))}>Reopen room</Button>
          ) : editable ? (
            <Button size="lg" className="flex-1 sm:flex-none" loading={busy === "complete"} onClick={() => void complete()}>Mark room complete</Button>
          ) : null}
          {room.nextRoomId && <Link href={`/agent/inspections/${inspectionId}/rooms/${room.nextRoomId}`} className="rounded-lg border border-slate-300 px-3 py-3 text-sm">→</Link>}
        </div>
        <p className="px-4 pb-2 text-center text-xs text-slate-500 sm:hidden">{SaveIndicatorText(autosave.state, autosave.error)}</p>
      </div>
    </div>
  );
}
