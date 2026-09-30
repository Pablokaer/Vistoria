"use client";

import { useCallback, useEffect, useRef, useState } from "react";

export type SaveState = "idle" | "dirty" | "saving" | "saved" | "error";

/**
 * Debounced autosave. `save` receives the latest value; failures are retried on the next change
 * or via `flush()`. Returns the current save state for UI feedback.
 */
export function useAutosave<T>(value: T, save: (value: T) => Promise<void>, { delay = 1200, enabled = true, savedValue }: { delay?: number; enabled?: boolean; savedValue?: T } = {}) {
  const [state, setState] = useState<SaveState>("idle");
  const [error, setError] = useState<string | null>(null);
  const latest = useRef(value);
  // `savedValue` = what the server already has (lets a restored local draft start as unsaved).
  const saved = useRef(JSON.stringify(savedValue ?? value));
  const saveRef = useRef(save);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const inFlight = useRef<Promise<void> | null>(null);

  useEffect(() => { saveRef.current = save; }, [save]);

  const flush = useCallback(async () => {
    if (timer.current) { clearTimeout(timer.current); timer.current = null; }
    if (inFlight.current) await inFlight.current;
    const snapshot = JSON.stringify(latest.current);
    if (snapshot === saved.current) return;
    setState("saving");
    const run = saveRef.current(latest.current)
      .then(() => { saved.current = snapshot; setError(null); setState(JSON.stringify(latest.current) === snapshot ? "saved" : "dirty"); })
      .catch((e: unknown) => { setState("error"); setError(e instanceof Error ? e.message : "Could not save"); throw e; });
    inFlight.current = run.finally(() => { inFlight.current = null; }).catch(() => undefined);
    await run;
  }, []);

  useEffect(() => {
    latest.current = value;
    if (!enabled || JSON.stringify(value) === saved.current) return;
    setState("dirty");
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => { void flush().catch(() => undefined); }, delay);
  }, [value, delay, enabled, flush]);

  useEffect(() => () => { if (timer.current) clearTimeout(timer.current); }, []);

  /** Mark a value as already persisted (e.g. after loading from the server). */
  const markSaved = useCallback((v: T) => { saved.current = JSON.stringify(v); latest.current = v; setState("idle"); }, []);

  return { state, error, flush, markSaved };
}

export function SaveIndicatorText(state: SaveState, error: string | null): string {
  switch (state) {
    case "dirty": return "Unsaved changes…";
    case "saving": return "Saving…";
    case "saved": return "All changes saved";
    case "error": return `Not saved — ${error ?? "retrying on next change"}`;
    default: return "";
  }
}
