"use client";

import { useState } from "react";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { errorMessage, get, post } from "@/lib/api";
import type { AiAnalysis } from "@/lib/types";
import { Button, Spinner } from "./ui";

/** Requests an AI analysis and polls until it finishes (analyses run in the background on the server). */
export function AiAnalysisButton({ requestUrl, statusUrlBase, latest, disabled, label, onDone }: {
  requestUrl: string; statusUrlBase: string; latest: AiAnalysis | null; disabled?: boolean; label: string; onDone: () => void | Promise<void>;
}) {
  const t = useT(inspectionToolsMessages);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setRunning(true);
    setError(null);
    try {
      let analysis = await post<AiAnalysis>(requestUrl);
      const started = Date.now();
      while ((analysis.status === "Pending" || analysis.status === "Processing") && Date.now() - started < 120_000) {
        await new Promise((r) => setTimeout(r, 1500));
        analysis = await get<AiAnalysis>(`${statusUrlBase}/${analysis.id}`);
      }
      if (analysis.status === "Failed") setError(analysis.error ?? t("aiFailed"));
      else if (analysis.status !== "Completed") setError(t("aiSlow"));
      await onDone();
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setRunning(false);
    }
  }

  const failedBefore = !running && !error && latest?.status === "Failed";
  return (
    <div>
      <Button type="button" variant="secondary" onClick={() => void run()} disabled={disabled || running}>
        {running ? <><Spinner small /> {t("analysingPhotos")}</> : `✨ ${label}`}
      </Button>
      {latest?.isMock && <span className="ml-2 rounded bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">{t("mockAi")}</span>}
      {(error || failedBefore) && <p className="mt-2 text-sm text-red-600">{error ?? latest?.error}</p>}
    </div>
  );
}
