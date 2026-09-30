"use client";

import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { ExecutionBar, RoomStepper } from "@/components/inspection/ExecutionBar";
import { ExecutionLayout, IssueList } from "@/components/inspection/ExecutionLayout";
import { RoomChipStrip, useCurrentRoom, type RoomNavItem } from "@/components/inspection/RoomNavigation";
import { RoomTexts, type RegisterFlush } from "@/components/inspection/RoomTexts";
import { SaveReportProvider, SaveStatus, useSaveStatusCollector } from "@/components/inspection/SaveStatus";
import { StepHeader } from "@/components/inspection/StepHeader";
import { Button, ErrorBanner, LinkButton, Notice, PageSkeleton, Spinner } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { errorMessage, post } from "@/lib/api";
import { hasRunningAnalysis, roomTextReady, roomUrl, useInspectionRooms } from "@/lib/rooms";
import type { RoomDetail } from "@/lib/types";

const navState = (r: RoomDetail): RoomNavItem["state"] =>
  hasRunningAnalysis(r) ? "writing" : r.status === "Completed" ? "done" : roomTextReady(r) ? "ready" : "pending";

const navItems = (rooms: RoomDetail[]): RoomNavItem[] => rooms.map((r) => ({ id: r.id, name: r.name, sequence: r.sequence, state: navState(r) }));

/** Poll rooms whose AI texts are still being written. */
function usePollWritingRooms(rooms: RoomDetail[] | null, reloadRoom: (roomId: string) => Promise<void>) {
  const pending = rooms?.filter(hasRunningAnalysis).map((r) => r.id).join(",") ?? "";
  useEffect(() => {
    if (!pending) return;
    const timer = setTimeout(() => { void Promise.all(pending.split(",").map((roomId) => reloadRoom(roomId).catch(() => undefined))); }, 2000);
    return () => clearTimeout(timer);
  }, [pending, rooms, reloadRoom]);
}

/** Completes every room (after saving pending edits), collecting what blocks each one; returns the problems. */
async function completeRooms(inspectionId: string, rooms: RoomDetail[], setRoom: (r: RoomDetail) => void): Promise<string[]> {
  const problems: string[] = [];
  for (const r of rooms) {
    if (r.status === "Completed") continue;
    try { setRoom(await post<RoomDetail>(`${roomUrl(inspectionId, r.id)}/complete`)); }
    catch (e) {
      const details = (e as { details?: string[] }).details;
      problems.push(...(details?.length ? details : [`${r.name}: ${errorMessage(e)}`]));
    }
  }
  return problems;
}

/**
 * Step 2 of the inspection: the AI texts requested on the photos page arrive here, room by room.
 * The agent reviews and edits them, then completes every room and moves on to the review.
 */
export default function DescriptionsPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const t = useT(captureMessages);
  const { inspection, rooms, error, load, setRoom, reloadRoom } = useInspectionRooms(id);
  const flushers = useRef(new Map<string, () => Promise<void>>());
  const [saveStatus, reportSave] = useSaveStatusCollector();
  const [busy, setBusy] = useState(false);
  const [issues, setIssues] = useState<string[]>([]);
  const [actionError, setActionError] = useState<string | null>(null);
  const currentId = useCurrentRoom(rooms?.map((r) => r.id) ?? []);
  const registerFlush = useCallback<RegisterFlush>((key, flush) => {
    flushers.current.set(key, flush);
    return () => { flushers.current.delete(key); };
  }, []);
  usePollWritingRooms(rooms, reloadRoom);

  if (!rooms || !inspection) return error ? <ErrorBanner message={error} onRetry={() => void load()} /> : <PageSkeleton />;

  const writing = rooms.filter(hasRunningAnalysis).length;
  const nav = navItems(rooms);
  const ready = nav.filter((n) => n.state === "ready" || n.state === "done").length;

  async function completeAll() {
    setBusy(true);
    setActionError(null);
    setIssues([]);
    try {
      await Promise.all([...flushers.current.values()].map((f) => f()));
      const problems = await completeRooms(id, rooms!, setRoom);
      if (problems.length > 0) { setIssues(problems); window.scrollTo({ top: 0, behavior: "smooth" }); return; }
      await post(`/api/agent/inspections/${id}/submit-review`);
      router.push(`/agent/inspections/${id}/review`);
    } catch (e) {
      setActionError(errorMessage(e));
    } finally { setBusy(false); }
  }

  const action = inspection.status === "InProgress"
    ? <Button size="lg" icon="arrowRight" loading={busy} disabled={writing > 0} onClick={() => void completeAll()}>{t("completeAndReview")}</Button>
    : inspection.status === "Review" ? <LinkButton size="lg" icon="arrowRight" href={`/agent/inspections/${id}/review`}>{t("continueReview")}</LinkButton> : null;

  return (
    <SaveReportProvider report={reportSave}>
      <StepHeader inspection={inspection} step={2} title={t("descriptionsTitle")} status={<SaveStatus value={saveStatus} />}
        progress={{ value: ready, total: rooms.length, label: t("progressReady", { value: ready, total: rooms.length }) }}
        rooms={<RoomChipStrip items={nav} currentId={currentId} />} />
      <ExecutionLayout nav={nav} currentId={currentId}>
        {writing > 0 && <Notice tone="info"><span className="flex items-center gap-2"><Spinner small /> {t("writingRooms", { count: writing })}</span></Notice>}
        <ErrorBanner message={actionError} />
        <IssueList title={t("cannotComplete")} issues={issues} />
        {rooms.map((r) => (
          <RoomTexts key={r.id} room={r} inspectionId={id} onRoom={setRoom} reload={() => reloadRoom(r.id)} registerFlush={registerFlush} />
        ))}
      </ExecutionLayout>
      <ExecutionBar stepper={<RoomStepper items={nav} currentId={currentId} />} hint={writing > 0 ? t("waitForAi") : t("reviewAiText")}>
        {action}
      </ExecutionBar>
    </SaveReportProvider>
  );
}
