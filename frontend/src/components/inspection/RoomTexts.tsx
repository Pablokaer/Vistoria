"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { AiAnalysisButton } from "@/components/AiAnalysisButton";
import { ComparisonPanel, RoomBeforeAfter } from "@/components/ComparisonPanel";
import { DefectEditor } from "@/components/DefectEditor";
import { Icon } from "@/components/icons";
import { Field, Skeleton, Spinner, Textarea } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { get, put } from "@/lib/api";
import { useAutosave } from "@/lib/autosave";
import { hasRunningAnalysis, roomUrl } from "@/lib/rooms";
import type { RoomDetail } from "@/lib/types";
import { AiTextEditor, aiTextState } from "./AiTextEditor";
import { PhotoGallery } from "./PhotoGallery";
import { RoomSection } from "./RoomSection";
import { SaveStatus, useReportSaveStatus } from "./SaveStatus";

export type RegisterFlush = (key: string, flush: () => Promise<void>) => () => void;
interface RoomTextsProps { room: RoomDetail; inspectionId: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void>; registerFlush: RegisterFlush }

interface Fields { finalDescription: string; agentNotes: string }
const fromRoom = (r: RoomDetail): Fields => ({ finalDescription: r.finalDescription ?? "", agentNotes: r.agentNotes ?? "" });

/** Placeholder shaped like the form while the AI writes the room's texts. */
function WritingPlaceholder({ room }: { room: RoomDetail }) {
  const t = useT(captureMessages);
  return (
    <RoomSection room={room}>
      <p className="flex items-center gap-2 text-label font-medium text-review-700">
        <Spinner small />{t("writingFromPhotos", { count: room.generalPhotos.length })}{room.defects.length > 0 && t("writingAndDefects", { count: room.defects.length })}…
      </p>
      <PhotoGallery urls={room.generalPhotos.map((p) => p.url)} size="sm" />
      <div className="space-y-2" aria-hidden="true"><Skeleton className="h-4 w-40" /><Skeleton className="h-28" /></div>
    </RoomSection>
  );
}

/** Shows a placeholder while the room's AI texts are being written, then the editable form. */
export function RoomTexts(props: RoomTextsProps) {
  const { room } = props;
  // Once shown, the form stays mounted (a later AI re-run must not discard what the agent is typing).
  const [ready, setReady] = useState(() => !hasRunningAnalysis(room));
  if (!ready && !hasRunningAnalysis(room)) setReady(true);
  return ready ? <RoomTextForm {...props} /> : <WritingPlaceholder room={room} />;
}

/** Autosaved description + notes of a room, with the AI pre-fill adopted only while the agent has written nothing. */
function useRoomFields({ room, inspectionId, onRoom, registerFlush }: RoomTextsProps) {
  const base = roomUrl(inspectionId, room.id);
  const [fields, setFields] = useState<Fields>(() => fromRoom(room));
  const defectsFound = useRef(room.defectsFound);
  useEffect(() => { defectsFound.current = room.defectsFound; }, [room.defectsFound]);
  const autosave = useAutosave(fields, async (v) => {
    onRoom(await put<RoomDetail>(base, { finalDescription: v.finalDescription || null, defectsFound: defectsFound.current, agentNotes: v.agentNotes || null }));
  }, { enabled: room.editable, savedValue: fromRoom(room) });
  const { flush } = autosave;
  useEffect(() => registerFlush(`room:${room.id}`, flush), [registerFlush, room.id, flush]);
  useReportSaveStatus(`room:${room.id}`, autosave.state, autosave.error);
  const set = <K extends keyof Fields>(k: K, v: Fields[K]) => setFields((f) => ({ ...f, [k]: v }));

  async function afterRoomAi() {
    const fresh = await get<RoomDetail>(base);
    onRoom(fresh);
    if (!fields.finalDescription.trim() && fresh.finalDescription) {
      const next = { ...fields, finalDescription: fresh.finalDescription };
      autosave.markSaved(next);
      setFields(next);
    }
  }
  return { base, fields, set, autosave, afterRoomAi };
}

/** What still stands between this room and "completed" — a neutral checklist, not an error (it is normal while working). */
function CompletionIssues({ room }: { room: RoomDetail }) {
  const t = useT(captureMessages);
  if (room.completionIssues.length === 0 || room.status === "Completed") return null;
  return (
    <div className="rounded-md bg-surface-2 px-3 py-2.5 text-label ring-1 ring-inset ring-line">
      <p className="mb-1 font-medium text-ink-2">{t("toComplete")}</p>
      <ul className="space-y-1 text-ink-3">
        {room.completionIssues.map((i) => <li key={i} className="flex gap-2"><Icon name="circleDashed" className="mt-0.5 h-4 w-4 shrink-0 text-ink-4" />{i}</li>)}
      </ul>
    </div>
  );
}

function RoomTextForm(props: RoomTextsProps) {
  const { room, inspectionId, onRoom, reload, registerFlush } = props;
  const t = useT(captureMessages);
  const { base, fields, set, autosave, afterRoomAi } = useRoomFields(props);
  const editable = room.editable;
  const state = aiTextState({ value: fields.finalDescription, aiText: room.aiDescription, writing: hasRunningAnalysis(room), reviewed: room.status === "Completed" });
  const editPhotos = editable && (
    <Link href={`/agent/inspections/${inspectionId}/capture#room-${room.id}`} className="inline-flex items-center gap-1 text-label font-medium text-brand hover:text-brand-dark">
      <Icon name="camera" className="h-4 w-4" />{t("editPhotos")}
    </Link>
  );
  return (
    <RoomSection room={room} aside={editPhotos}>
      {room.comparison?.baseline ? <RoomBeforeAfter room={room} currentText={fields.finalDescription} /> : <PhotoGallery urls={room.generalPhotos.map((p) => p.url)} />}
      <AiTextEditor label={t("roomDescriptionLabel")} hint={t("roomDescriptionHint")} rows={6} disabled={!editable}
        value={fields.finalDescription} onChange={(v) => set("finalDescription", v)} aiText={room.aiDescription} state={state}
        tools={<AiAnalysisButton label={t("regenerateAi")} requestUrl={`${base}/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={room.latestAnalysis} disabled={!editable || room.generalPhotos.length === 0} onDone={afterRoomAi} />} />
      {room.defects.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-label font-semibold text-ink-2">{t("defects")} <span className="tabular text-ink-3">({room.defects.length})</span></h3>
          {room.defects.map((d, i) => (
            <DefectEditor key={d.id} defect={d} index={i} base={base} editable={editable} moveOut={!!room.comparison} onRoom={onRoom} reload={reload} registerFlush={registerFlush} />
          ))}
        </div>
      )}
      {room.comparison && <ComparisonPanel room={room} base={base} onRoom={onRoom} reload={reload} />}
      <Field label={t("agentNotes")}>
        <Textarea rows={2} disabled={!editable} value={fields.agentNotes} onChange={(e) => set("agentNotes", e.target.value)} placeholder={t("agentNotesPlaceholder")} />
      </Field>
      <CompletionIssues room={room} />
      <SaveStatus value={{ state: autosave.state, error: autosave.error }} />
    </RoomSection>
  );
}
