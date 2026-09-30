"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { Button, Field, Textarea } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";

export type ResponseMode = "choose" | "observe" | "dispute";

interface Props {
  busy: string | null;
  onAccept: () => void;
  onGeneralObservation: (text: string) => Promise<boolean>;
  onDispute: (reason: string) => void;
}

function Steps() {
  const t = useT(tenantMessages);
  return (
    <ol className="hidden space-y-2 xl:block">
      {(["step1", "step2", "step3"] as const).map((key, i) => (
        <li key={key} className="flex gap-2.5 text-label text-ink-2">
          <span className="tabular grid h-5 w-5 shrink-0 place-items-center rounded-full bg-brand-50 text-caption font-semibold text-brand">{i + 1}</span>{t(key)}
        </li>
      ))}
    </ol>
  );
}

function GeneralObservationForm({ busy, onSubmit, onDone }: { busy: boolean; onSubmit: (text: string) => Promise<boolean>; onDone: () => void }) {
  const t = useT(tenantMessages);
  const [text, setText] = useState("");
  return (
    <div className="space-y-3">
      <Field label={t("generalObservation")} hint={t("generalObservationHint")}><Textarea rows={3} value={text} onChange={(e) => setText(e.target.value)} /></Field>
      <div className="flex flex-wrap gap-2">
        <Button loading={busy} disabled={!text.trim()} onClick={() => void onSubmit(text).then((ok) => { if (ok) { setText(""); onDone(); } })}>{t("addObservation")}</Button>
        <Button variant="secondary" onClick={onDone}>{t("done")}</Button>
      </div>
    </div>
  );
}

function DisputeForm({ busy, onSubmit, onBack }: { busy: boolean; onSubmit: (reason: string) => void; onBack: () => void }) {
  const t = useT(tenantMessages);
  const [reason, setReason] = useState("");
  return (
    <div className="space-y-3">
      <Field label={t("disagreeWhat")} hint={t("disagreeHint")}><Textarea rows={3} value={reason} onChange={(e) => setReason(e.target.value)} /></Field>
      <div className="flex flex-wrap gap-2">
        <Button variant="danger" icon="flag" loading={busy} disabled={!reason.trim()} onClick={() => onSubmit(reason)}>{t("disputeReport")}</Button>
        <Button variant="secondary" onClick={onBack}>{t("back")}</Button>
      </div>
    </div>
  );
}

function Choices({ busy, onAccept, onMode }: { busy: boolean; onAccept: () => void; onMode: (mode: ResponseMode) => void }) {
  const t = useT(tenantMessages);
  return (
    <div className="grid grid-cols-1 gap-2 sm:grid-cols-3 xl:grid-cols-1">
      <Button size="lg" icon="checkCircle" loading={busy} onClick={onAccept}>{t("everythingCorrect")}</Button>
      <Button size="lg" variant="secondary" icon="message" onClick={() => onMode("observe")}>{t("wantObservations")}</Button>
      <Button size="lg" variant="danger" icon="flag" onClick={() => onMode("dispute")}>{t("disagree")}</Button>
    </div>
  );
}

/**
 * The tenant's answer to a report. One element for every screen size: a fixed bottom sheet on phones/tablets,
 * a sticky panel beside the document on desktop — so each action exists exactly once on the page.
 */
export function TenantResponsePanel({ busy, onAccept, onGeneralObservation, onDispute }: Props) {
  const t = useT(tenantMessages);
  const [mode, setMode] = useState<ResponseMode>("choose");
  return (
    <section aria-labelledby="tenant-response"
      className="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-surface/95 px-4 py-3 shadow-overlay backdrop-blur lg:left-sidebar xl:static xl:rounded-lg xl:border xl:bg-surface xl:p-4 xl:shadow-card">
      <div className="mx-auto max-w-page space-y-3 xl:max-w-none">
        <h2 id="tenant-response" className="flex items-center gap-2 text-label font-semibold text-ink xl:text-section">
          <Icon name="userCheck" className="h-4 w-4 text-brand" />{t("yourResponse")}
        </h2>
        {mode === "choose" && <><Steps /><Choices busy={busy === "accept"} onAccept={onAccept} onMode={setMode} /></>}
        {mode === "observe" && <GeneralObservationForm busy={busy === "general"} onSubmit={onGeneralObservation} onDone={() => setMode("choose")} />}
        {mode === "dispute" && <DisputeForm busy={busy === "dispute"} onSubmit={onDispute} onBack={() => setMode("choose")} />}
      </div>
    </section>
  );
}
