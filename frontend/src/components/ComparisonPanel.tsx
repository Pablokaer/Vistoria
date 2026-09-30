"use client";

import { useState } from "react";
import { BeforeAfter } from "@/components/inspection/BeforeAfter";
import { Icon } from "@/components/icons";
import { PhotoGallery } from "@/components/inspection/PhotoGallery";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { errorMessage, post, put } from "@/lib/api";
import { cx } from "@/lib/cx";
import type { BaselineRoom, ComparisonDecision, RoomDetail } from "@/lib/types";
import { AiAnalysisButton } from "./AiAnalysisButton";
import { Button, Field, Textarea, useToast } from "./ui";

const DECISIONS: ComparisonDecision[] = ["Unchanged", "NormalWear", "NewDamage", "PreExisting", "Resolved", "UnableToDetermine"];

function BaselineDefects({ defects }: { defects: BaselineRoom["defects"] }) {
  const t = useT(inspectionToolsMessages);
  if (defects.length === 0) return null;
  return (
    <div className="mt-3 space-y-2">
      <p className="text-caption font-semibold uppercase tracking-wide text-ink-3">{t("moveInDefects")}</p>
      {defects.map((d, i) => (
        <div key={i} className="rounded-md bg-surface p-2.5 text-label ring-1 ring-inset ring-line">
          <p className="font-medium text-ink">{d.title ?? t("defect")} {d.location && <span className="font-normal text-ink-3">· {d.location}</span>}</p>
          {d.description && <p className="mt-0.5 text-ink-2">{d.description}</p>}
          {d.photoUrls.length > 0 && <div className="mt-2"><PhotoGallery urls={d.photoUrls} size="sm" /></div>}
        </div>
      ))}
    </div>
  );
}

/** Read-only Move In record of the room (shown on the photos step, where the inspector re-photographs it). */
export function BaselinePanel({ room }: { room: RoomDetail }) {
  const t = useT(inspectionToolsMessages);
  const { formatDate } = useFormatters();
  const b = room.comparison?.baseline;
  if (!b) return null;
  return (
    <div className="rounded-md bg-movein-50/60 p-3 ring-1 ring-inset ring-movein/20">
      <p className="mb-2 flex flex-wrap items-center gap-2 text-label font-semibold text-movein">
        <Icon name="doorIn" className="h-4 w-4" />{t("moveInRecordTitle", { room: b.name })}
        <span className="font-normal text-ink-3">{t("baselineMeta", { number: b.reportNumber ?? "", date: formatDate(b.completedAt) })}</span>
      </p>
      <p className="mb-3 whitespace-pre-line text-label text-ink-2">{b.description ?? t("noDescription")}</p>
      <PhotoGallery urls={b.photoUrls} size="sm" />
      <BaselineDefects defects={b.defects} />
      {b.agentNotes && <p className="mt-3 text-label text-ink-2"><span className="font-medium">{t("notesLabel")}</span> {b.agentNotes}</p>}
    </div>
  );
}

/** Before/after of the room: Move In record vs what the inspector is recording now. */
export function RoomBeforeAfter({ room, currentText }: { room: RoomDetail; currentText: string }) {
  const t = useT(inspectionToolsMessages);
  const { formatDate } = useFormatters();
  const b = room.comparison?.baseline;
  if (!b) return null;
  return (
    <BeforeAfter
      before={{ photos: b.photoUrls, text: b.description, meta: t("baselineMeta", { number: b.reportNumber ?? "", date: formatDate(b.completedAt) }), extra: <BaselineDefects defects={b.defects} /> }}
      after={{ photos: room.generalPhotos.map((p) => p.url), text: currentText }} />
  );
}

function PossibleDifferences({ basic, ai }: { basic: string | null; ai: string | null }) {
  const t = useT(inspectionToolsMessages);
  if (!basic && !ai) return null;
  return (
    <div className="mt-3 space-y-2">
      {basic && <pre className="whitespace-pre-wrap rounded-md bg-surface-2 p-3 font-sans text-label text-ink-2 ring-1 ring-inset ring-line">{basic}</pre>}
      {ai && (
        <div className="rounded-md bg-review-50/60 p-3 text-label ring-1 ring-inset ring-review-700/15">
          <p className="mb-1 flex items-center gap-1.5 font-semibold text-review-700"><Icon name="sparkles" className="h-4 w-4" />{t("aiSuggestion")}</p>
          <p className="whitespace-pre-line text-ink-2">{ai}</p>
        </div>
      )}
    </div>
  );
}

function DecisionPicker({ value, onChange, disabled }: { value: ComparisonDecision | null; onChange: (d: ComparisonDecision) => void; disabled: boolean }) {
  const t = useT(inspectionToolsMessages);
  const { humanize } = useFormatters();
  return (
    <fieldset>
      <legend className="mb-2 text-label font-medium text-ink-2">{t("yourDecision")}</legend>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
        {DECISIONS.map((d) => (
          <button key={d} type="button" disabled={disabled} aria-pressed={value === d} onClick={() => onChange(d)}
            className={cx("min-h-11 rounded-md border px-3 text-label font-medium transition active:scale-[0.98]",
              value === d ? "border-brand bg-brand text-white shadow-card" : "border-line-strong bg-surface text-ink-2 hover:border-ink-4")}>{humanize(d)}</button>
        ))}
      </div>
    </fieldset>
  );
}

/** The inspector's comparison: tools that suggest differences, then the decision (theirs alone) and notes. */
export function ComparisonPanel({ room, base, onRoom, reload }: { room: RoomDetail; base: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void> }) {
  const t = useT(inspectionToolsMessages);
  const { humanize } = useFormatters();
  const toast = useToast();
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
  const saveDecision = () => act("decide", async () => {
    onRoom(await put<RoomDetail>(`${base}/comparison/decision`, { decision, notes: notes || null }));
    toast.notify({ tone: "success", title: t("decisionSaved") });
  });

  return (
    <div className="rounded-lg border border-line bg-surface p-4">
      <h3 className="flex items-center gap-2 text-card-title font-semibold text-ink"><Icon name="compare" className="h-4.5 w-4.5 text-ink-3" />{t("compareWithMoveIn")}</h3>
      <p className="mt-1 text-label text-ink-3">{t("compareIntroLead")}<strong className="text-ink-2">{t("compareIntroYou")}</strong>{t("compareIntroTail")}</p>
      <p className="mt-3 text-caption font-semibold uppercase tracking-wide text-ink-3">{t("possibleDifferences")}</p>
      <div className="mt-1.5 flex flex-wrap items-center gap-2">
        <Button variant="secondary" size="sm" icon="compare" disabled={!editable} loading={busy === "basic"} onClick={() => act("basic", async () => onRoom(await post<RoomDetail>(`${base}/comparison/basic`)))}>
          {t("compareRecorded")}
        </Button>
        <AiAnalysisButton label={t("aiVisualComparison")} requestUrl={`${base}/comparison/analysis`} statusUrlBase={base.replace(/\/rooms\/[^/]+$/, "/analyses")}
          latest={c.latestAnalysis} disabled={!editable || room.generalPhotos.length === 0} onDone={reload} />
      </div>
      <PossibleDifferences basic={c.basicComparison} ai={c.aiAnalysis} />
      <div className="mt-4"><DecisionPicker value={decision} onChange={setDecision} disabled={!editable} /></div>
      <div className="mt-3"><Field label={t("comparisonNotes")}><Textarea rows={2} disabled={!editable} value={notes} onChange={(e) => setNotes(e.target.value)} /></Field></div>
      {error && <p role="alert" className="mt-2 text-label text-danger-700">{error}</p>}
      <div className="mt-3 flex flex-wrap items-center gap-3">
        <Button disabled={!editable || !decision} loading={busy === "decide"} onClick={() => void saveDecision()}>{t("saveDecision")}</Button>
        {c.agentDecision && (
          <span className="inline-flex items-center gap-1.5 text-label font-medium text-success-700">
            <Icon name="checkCircle" className="h-4 w-4" />{t("savedDecision", { decision: humanize(c.agentDecision) })}
          </span>
        )}
      </div>
    </div>
  );
}
