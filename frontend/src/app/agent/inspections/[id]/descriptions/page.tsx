"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { AiAnalysisButton } from "@/components/AiAnalysisButton";
import { ComparisonPanel } from "@/components/ComparisonPanel";
import { DefectEditor } from "@/components/DefectEditor";
import { PhotoStrip } from "@/components/PhotoUploader";
import { Badge, Button, Card, ErrorBanner, Field, Loading, Notice, PageHeader, Spinner, Textarea } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { autosaveMessages } from "@/i18n/messages/inspectionTools";
import { errorMessage, get, post, put } from "@/lib/api";
import { SaveIndicatorText, useAutosave } from "@/lib/autosave";
import { hasRunningAnalysis, roomUrl, useInspectionRooms } from "@/lib/rooms";
import type { RoomDetail } from "@/lib/types";

type RegisterFlush = (key: string, flush: () => Promise<void>) => () => void;

/**
 * Step 2 of the inspection: the AI texts requested on the photos page arrive here, room by room.
 * The agent reviews and edits them, then completes every room and moves on to the review.
 */
export default function DescriptionsPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const t = useT(captureMessages);
  const { humanize } = useFormatters();
  const { inspection, rooms, error, load, setRoom, reloadRoom } = useInspectionRooms(id);
  const flushers = useRef(new Map<string, () => Promise<void>>());
  const [busy, setBusy] = useState(false);
  const [issues, setIssues] = useState<string[]>([]);
  const [actionError, setActionError] = useState<string | null>(null);

  const registerFlush = useCallback<RegisterFlush>((key, flush) => {
    flushers.current.set(key, flush);
    return () => { flushers.current.delete(key); };
  }, []);

  // Poll rooms whose AI texts are still being written.
  const pending = rooms?.filter(hasRunningAnalysis).map((r) => r.id).join(",") ?? "";
  useEffect(() => {
    if (!pending) return;
    const t = setTimeout(() => { void Promise.all(pending.split(",").map((roomId) => reloadRoom(roomId).catch(() => undefined))); }, 2000);
    return () => clearTimeout(t);
  }, [pending, rooms, reloadRoom]);

  if (!rooms || !inspection) return error ? <ErrorBanner message={error} onRetry={() => void load()} /> : <Loading />;

  const writing = rooms.filter(hasRunningAnalysis).length;
  const inProgress = inspection.status === "InProgress";

  async function completeAll() {
    setBusy(true);
    setActionError(null);
    setIssues([]);
    try {
      await Promise.all([...flushers.current.values()].map((f) => f()));
      const problems: string[] = [];
      for (const r of rooms!) {
        if (r.status === "Completed") continue;
        try { setRoom(await post<RoomDetail>(`${roomUrl(id, r.id)}/complete`)); }
        catch (e) {
          const details = (e as { details?: string[] }).details;
          problems.push(...(details?.length ? details : [`${r.name}: ${errorMessage(e)}`]));
        }
      }
      if (problems.length > 0) {
        setIssues(problems);
        window.scrollTo({ top: 0, behavior: "smooth" });
        return;
      }
      await post(`/api/agent/inspections/${id}/submit-review`);
      router.push(`/agent/inspections/${id}/review`);
    } catch (e) {
      setActionError(errorMessage(e));
    } finally { setBusy(false); }
  }

  return (
    <div className="pb-28">
      <PageHeader title={t("descriptionsTitle")} subtitle={t("descriptionsSubtitle", { type: humanize(inspection.inspectionType) })}
        back={{ href: `/agent/inspections/${id}/capture`, label: t("photosTitle") }} />
      {writing > 0 && <Notice tone="info"><span className="flex items-center gap-2"><Spinner small /> {t("writingRooms", { count: writing })}</span></Notice>}
      <ErrorBanner message={actionError} />
      {issues.length > 0 && (
        <div className="mb-4 rounded-lg bg-red-50 px-4 py-3 text-sm text-red-800">
          <strong>{t("cannotComplete")}</strong>
          <ul className="mt-1 list-disc space-y-1 pl-5">{issues.map((i) => <li key={i}>{i}</li>)}</ul>
        </div>
      )}

      <div className="space-y-6">
        {rooms.map((r) => (
          <RoomTexts key={r.id} room={r} inspectionId={id} onRoom={setRoom} reload={() => reloadRoom(r.id)} registerFlush={registerFlush} />
        ))}
      </div>

      <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-end gap-3 px-4 py-3">
          <span className="flex-1 text-xs text-slate-500">{writing > 0 ? t("waitForAi") : t("reviewAiText")}</span>
          {inProgress ? (
            <Button size="lg" loading={busy} disabled={writing > 0} onClick={() => void completeAll()}>{t("completeAndReview")}</Button>
          ) : inspection.status === "Review" ? (
            <Link href={`/agent/inspections/${id}/review`} className="rounded-lg bg-brand px-4 py-3 text-sm font-medium text-white">{t("continueReview")}</Link>
          ) : null}
        </div>
      </div>
    </div>
  );
}

/** Shows a placeholder while the room's AI texts are being written, then the editable form. */
function RoomTexts(props: { room: RoomDetail; inspectionId: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void>; registerFlush: RegisterFlush }) {
  const { room } = props;
  const t = useT(captureMessages);
  // Once shown, the form stays mounted (a later AI re-run must not discard what the agent is typing).
  const [ready, setReady] = useState(() => !hasRunningAnalysis(room));
  if (!ready && !hasRunningAnalysis(room)) setReady(true);

  if (!ready) {
    return (
      <Card title={`${room.sequence}. ${room.name}`}>
        <div className="flex items-center gap-3 text-sm text-slate-600"><Spinner small /> {t("writingFromPhotos", { count: room.generalPhotos.length })}
          {room.defects.length > 0 && t("writingAndDefects", { count: room.defects.length })}…</div>
      </Card>
    );
  }
  return <RoomTextForm {...props} />;
}

interface Fields { finalDescription: string; agentNotes: string }
const fromRoom = (r: RoomDetail): Fields => ({ finalDescription: r.finalDescription ?? "", agentNotes: r.agentNotes ?? "" });

function RoomTextForm({ room, inspectionId, onRoom, reload, registerFlush }: {
  room: RoomDetail; inspectionId: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void>; registerFlush: RegisterFlush;
}) {
  const t = useT(captureMessages);
  const tSave = useT(autosaveMessages);
  const base = roomUrl(inspectionId, room.id);
  const [fields, setFields] = useState<Fields>(() => fromRoom(room));
  const editable = room.editable;
  const defectsFound = useRef(room.defectsFound);
  useEffect(() => { defectsFound.current = room.defectsFound; }, [room.defectsFound]);

  const autosave = useAutosave(fields, async (v) => {
    onRoom(await put<RoomDetail>(base, { finalDescription: v.finalDescription || null, defectsFound: defectsFound.current, agentNotes: v.agentNotes || null }));
  }, { enabled: editable, savedValue: fromRoom(room) });
  const { flush } = autosave;
  useEffect(() => registerFlush(`room:${room.id}`, flush), [registerFlush, room.id, flush]);

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

  return (
    <section id={`room-${room.id}`} className="scroll-mt-20">
      <Card title={<span className="flex flex-wrap items-center gap-3">{room.sequence}. {room.name} <Badge value={room.status} /></span>}
        actions={editable && <Link href={`/agent/inspections/${inspectionId}/capture#room-${room.id}`} className="text-sm text-brand hover:underline">{t("editPhotos")}</Link>}>
        <PhotoStrip urls={room.generalPhotos.map((p) => p.url)} />

        <div className="mt-4">
          <AiAnalysisButton label={t("regenerateAi")} requestUrl={`${base}/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
            latest={room.latestAnalysis} disabled={!editable || room.generalPhotos.length === 0} onDone={afterRoomAi} />
          {room.aiDescription && editable && fields.finalDescription !== room.aiDescription && (
            <details className="mt-3 rounded-lg bg-slate-50 p-3 text-sm">
              <summary className="cursor-pointer font-medium text-slate-700">{t("aiDraftKept")}</summary>
              <p className="mt-2 whitespace-pre-line text-slate-700">{room.aiDescription}</p>
              <Button variant="ghost" className="mt-1" onClick={() => set("finalDescription", room.aiDescription ?? "")}>{t("replaceWithAi")}</Button>
            </details>
          )}
          <div className="mt-3">
            <Field label={t("roomDescriptionLabel")} hint={t("roomDescriptionHint")}>
              <Textarea rows={6} disabled={!editable} value={fields.finalDescription} onChange={(e) => set("finalDescription", e.target.value)} />
            </Field>
          </div>
        </div>

        {room.defects.length > 0 && (
          <div className="mt-4 space-y-3">
            <h3 className="text-sm font-semibold text-slate-700">{t("defects")}</h3>
            {room.defects.map((d, i) => (
              <DefectEditor key={d.id} defect={d} index={i} base={base} editable={editable} moveOut={!!room.comparison} onRoom={onRoom} reload={reload} registerFlush={registerFlush} />
            ))}
          </div>
        )}

        {room.comparison && <div className="mt-4"><ComparisonPanel room={room} base={base} onRoom={onRoom} reload={reload} /></div>}

        <div className="mt-4">
          <Field label={t("agentNotes")}>
            <Textarea rows={2} disabled={!editable} value={fields.agentNotes} onChange={(e) => set("agentNotes", e.target.value)} placeholder={t("agentNotesPlaceholder")} />
          </Field>
        </div>

        {room.completionIssues.length > 0 && room.status !== "Completed" && (
          <ul className="mt-3 list-disc space-y-1 rounded-lg bg-amber-50 py-2 pl-8 pr-3 text-sm text-amber-900">{room.completionIssues.map((i) => <li key={i}>{i}</li>)}</ul>
        )}
        <p className="mt-2 text-xs text-slate-500">{SaveIndicatorText(autosave.state, autosave.error, tSave)}</p>
      </Card>
    </section>
  );
}
