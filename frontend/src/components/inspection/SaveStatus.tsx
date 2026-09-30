"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { Icon } from "@/components/icons";
import { Spinner } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { autosaveMessages } from "@/i18n/messages/inspectionTools";
import { SaveIndicatorText, type SaveState } from "@/lib/autosave";
import { cx } from "@/lib/cx";

export interface SaveStatusValue { state: SaveState; error: string | null }
type Report = (key: string, value: SaveStatusValue | null) => void;

const SaveReportContext = createContext<Report | null>(null);

// Worst state wins: an error anywhere must be visible in the page header, not only under one form.
const PRIORITY: SaveState[] = ["error", "saving", "dirty", "saved", "idle"];

/** Combines every autosaved form of the page into one status. */
export function aggregateSaveStatus(values: SaveStatusValue[]): SaveStatusValue {
  for (const state of PRIORITY) {
    const hit = values.find((v) => v.state === state);
    if (hit) return hit;
  }
  return { state: "idle", error: null };
}

/**
 * Collects the autosave state of every form below it. Example:
 * `const [status, provider] = useSaveStatusCollector(); return <SaveReportProvider report={provider}>…`
 */
export function useSaveStatusCollector(): [SaveStatusValue, Report] {
  const [values, setValues] = useState<Record<string, SaveStatusValue>>({});
  const report = useCallback<Report>((key, value) => setValues((all) => {
    if (value === null) { const rest = { ...all }; delete rest[key]; return rest; }
    const current = all[key];
    return current && current.state === value.state && current.error === value.error ? all : { ...all, [key]: value };
  }), []);
  const status = useMemo(() => aggregateSaveStatus(Object.values(values)), [values]);
  return [status, report];
}

export function SaveReportProvider({ report, children }: { report: Report; children: ReactNode }) {
  return <SaveReportContext.Provider value={report}>{children}</SaveReportContext.Provider>;
}

/** Forms call this with their autosave state; without a provider it does nothing. */
export function useReportSaveStatus(key: string, state: SaveState, error: string | null) {
  const report = useContext(SaveReportContext);
  useEffect(() => { report?.(key, { state, error }); }, [report, key, state, error]);
  useEffect(() => () => report?.(key, null), [report, key]);
}

/** Icon + text save indicator ("Saving…", "All changes saved", "Not saved — …"). Renders nothing when idle. */
export function SaveStatus({ value, className }: { value: SaveStatusValue; className?: string }) {
  const t = useT(autosaveMessages);
  if (value.state === "idle") return null;
  const tone = value.state === "error" ? "text-danger-700" : value.state === "saved" ? "text-success-700" : "text-ink-3";
  return (
    <span role="status" className={cx("inline-flex items-center gap-1.5 text-caption font-medium", tone, className)}>
      {value.state === "saving" ? <Spinner small /> : <Icon name={value.state === "error" ? "alert" : value.state === "saved" ? "checkCircle" : "edit"} className="h-4 w-4" />}
      {SaveIndicatorText(value.state, value.error, t)}
    </span>
  );
}
