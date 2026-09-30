"use client";

import { useState } from "react";
import { errorMessage, get, post } from "@/lib/api";
import type { AiAnalysis } from "@/lib/types";
import { Button, Spinner } from "./ui";

/** Requests an AI analysis and polls until it finishes (analyses run in the background on the server). */
export function AiAnalysisButton({ requestUrl, statusUrlBase, latest, disabled, label, onDone }: {
  requestUrl: string; statusUrlBase: string; latest: AiAnalysis | null; disabled?: boolean; label: string; onDone: () => void | Promise<void>;
}) {
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
      if (analysis.status === "Failed") setError(analysis.error ?? "The AI analysis failed. You can retry or write the text yourself.");
      else if (analysis.status !== "Completed") setError("The AI is taking longer than expected. Refresh in a moment.");
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
        {running ? <><Spinner small /> Analysing photos…</> : `✨ ${label}`}
      </Button>
      {latest?.isMock && <span className="ml-2 rounded bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-800">Development mock AI</span>}
      {(error || failedBefore) && <p className="mt-2 text-sm text-red-600">{error ?? latest?.error}</p>}
    </div>
  );
}
