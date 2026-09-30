"use client";

import { useRouter } from "next/navigation";
import { useState, type ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { Button, ErrorBanner, LinkButton, useToast } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { errorMessage, post } from "@/lib/api";
import type { InspectionDetails } from "@/lib/types";

function StepShell({ icon, title, body, tone, children }: { icon: IconName; title: string; body?: ReactNode; tone: string; children?: ReactNode }) {
  const t = useT(agentMessages);
  return (
    <section className="rounded-lg border border-line bg-surface p-4 shadow-card sm:p-5">
      <div className="flex items-start gap-3">
        <span className={`grid h-10 w-10 shrink-0 place-items-center rounded-lg ${tone}`}><Icon name={icon} className="h-5 w-5" /></span>
        <div className="min-w-0 flex-1">
          <p className="text-caption font-semibold uppercase tracking-wide text-ink-3">{t("nextStep")}</p>
          <h2 className="text-section font-semibold text-ink">{title}</h2>
          {body && <p className="mt-1 text-body text-ink-3">{body}</p>}
        </div>
      </div>
      {children && <div className="mt-4 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">{children}</div>}
    </section>
  );
}

/** The API calls behind the panel's buttons; navigation happens only after the call succeeded. */
function useStepActions(i: InspectionDetails) {
  const router = useRouter();
  const t = useT(agentMessages);
  const toast = useToast();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const run = async (call: () => Promise<unknown>, to: string, done?: string) => {
    setBusy(true);
    setError(null);
    try { await call(); if (done) toast.notify({ tone: "success", title: done }); router.push(to); }
    catch (e) { setError(errorMessage(e)); setBusy(false); }
  };
  const base = `/agent/inspections/${i.id}`;
  return {
    busy, error,
    start: () => run(() => post(`/api${base}/start`), `${base}/capture`, t("stepInProgressTitle")),
    review: () => run(() => (i.status === "InProgress" ? post(`/api${base}/submit-review`) : Promise.resolve()), `${base}/review`),
  };
}

/**
 * The single most relevant action for the inspection's state, with a one-line explanation. Button names are the
 * ones the rest of the product (and the e2e flow) refer to: "Start inspection", "Review inspection", …
 */
export function NextStepPanel({ inspection: i }: { inspection: InspectionDetails }) {
  const t = useT(agentMessages);
  const { formatDateTime } = useFormatters();
  const { busy, error, start, review } = useStepActions(i);
  const can = (a: string) => i.allowedActions.includes(a);
  const base = `/agent/inspections/${i.id}`;
  const errorBanner = <ErrorBanner message={error} />;

  if (i.status === "Assigned") return <>{errorBanner}<StepShell icon="play" tone="bg-brand-50 text-brand" title={t("stepAssignedTitle")} body={t("startHint")}>
    <Button size="lg" icon="play" className="w-full sm:w-auto" loading={busy} onClick={() => void start()}>{t("startInspection")}</Button>
  </StepShell></>;

  if (i.status === "InProgress") return <>{errorBanner}<StepShell icon="camera" tone="bg-warning-50 text-warning-700" title={t("stepInProgressTitle")}
    body={can("submitForReview") ? undefined : t("reviewHint")}>
    {can("edit") && i.roomsCompleted < i.rooms.length && <>
      <LinkButton href={`${base}/capture`} size="lg" icon="camera" className="w-full sm:w-auto">{t("photos")}</LinkButton>
      <LinkButton href={`${base}/descriptions`} size="lg" variant="secondary" icon="file" className="w-full sm:w-auto">{t("descriptions")}</LinkButton>
    </>}
    <Button size="lg" variant={can("submitForReview") ? "primary" : "secondary"} icon="eye" disabled={!can("submitForReview")} loading={busy}
      className="w-full sm:w-auto" onClick={() => void review()}>{t("reviewInspection")}</Button>
  </StepShell></>;

  if (i.status === "Review") return <StepShell icon="eye" tone="bg-review-50 text-review-700" title={t("stepReviewTitle")} body={t("stepReviewBody")}>
    <LinkButton href={`${base}/review`} size="lg" icon="eye" className="w-full sm:w-auto">{t("continueReview")}</LinkButton>
  </StepShell>;

  if (!i.completedAt) return null;
  return <StepShell icon="checkCircle" tone="bg-success-50 text-success-700" title={t("stepDoneTitle")} body={t("finalizedOn", { date: formatDateTime(i.completedAt) })}>
    {i.report && <LinkButton href={`/reports/${i.report.reportId}`} size="lg" icon="file" className="w-full sm:w-auto">{t("viewReport")}</LinkButton>}
  </StepShell>;
}
