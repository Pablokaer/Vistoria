"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { errorMessage, get, post } from "@/lib/api";
import type { AiAnalysis } from "@/lib/types";
import { Button, useToast } from "./ui";

const POLL_MS = 1500;
const GIVE_UP_MS = 120_000;

/** Polls a background analysis until it leaves Pending/Processing or the time budget runs out. */
async function waitForAnalysis(first: AiAnalysis, statusUrlBase: string): Promise<AiAnalysis> {
  let analysis = first;
  const started = Date.now();
  while ((analysis.status === "Pending" || analysis.status === "Processing") && Date.now() - started < GIVE_UP_MS) {
    await new Promise((r) => setTimeout(r, POLL_MS));
    analysis = await get<AiAnalysis>(`${statusUrlBase}/${analysis.id}`);
  }
  return analysis;
}

/** Requests an AI analysis and polls until it finishes (analyses run in the background on the server). */
export function AiAnalysisButton({ requestUrl, statusUrlBase, latest, disabled, label, onDone }: {
  requestUrl: string; statusUrlBase: string; latest: AiAnalysis | null; disabled?: boolean; label: string; onDone: () => void | Promise<void>;
}) {
  const t = useT(inspectionToolsMessages);
  const toast = useToast();
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setRunning(true);
    setError(null);
    try {
      const analysis = await waitForAnalysis(await post<AiAnalysis>(requestUrl), statusUrlBase);
      if (analysis.status === "Failed") setError(analysis.error ?? t("aiFailed"));
      else if (analysis.status !== "Completed") setError(t("aiSlow"));
      else toast.notify({ tone: "ai", title: t("aiReady") });
      await onDone();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setRunning(false);
    }
  }

  const failedBefore = !running && !error && latest?.status === "Failed";
  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button type="button" variant="secondary" size="sm" icon="sparkles" loading={running} onClick={() => void run()} disabled={disabled || running}>
        {running ? t("analysingPhotos") : label}
      </Button>
      {latest?.isMock && <span className="rounded-sm bg-warning-50 px-1.5 py-0.5 text-caption font-medium text-warning-700 ring-1 ring-inset ring-warning-700/20">{t("mockAi")}</span>}
      {(error || failedBefore) && (
        <p role="alert" className="flex w-full items-center gap-1.5 text-label text-danger-700"><Icon name="alert" className="h-4 w-4 shrink-0" />{error ?? latest?.error}</p>
      )}
    </div>
  );
}
