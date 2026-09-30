"use client";

import { useId, type ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { Button, Spinner, Textarea } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { cx } from "@/lib/cx";

export type AiTextState = "writing" | "draft" | "edited" | "manual" | "reviewed" | "empty";

/**
 * Where the text in the box came from. The inspector must see at a glance whether they are looking at an
 * untouched AI suggestion (to review) or at their own words.
 */
export function aiTextState({ value, aiText, writing, reviewed }: { value: string; aiText: string | null; writing: boolean; reviewed: boolean }): AiTextState {
  if (writing) return "writing";
  if (reviewed && value.trim()) return "reviewed";
  if (!value.trim()) return "empty";
  if (!aiText) return "manual";
  return value.trim() === aiText.trim() ? "draft" : "edited";
}

type ChipKey = "aiStateWriting" | "aiStateDraft" | "aiStateEdited" | "aiStateManual" | "aiStateReviewed";
const CHIP: Partial<Record<AiTextState, { icon: IconName; className: string; label: ChipKey }>> = {
  draft: { icon: "sparkles", className: "bg-review-50 text-review-700 ring-review-700/15", label: "aiStateDraft" },
  edited: { icon: "edit", className: "bg-neutral-50 text-neutral-700 ring-neutral-700/15", label: "aiStateEdited" },
  manual: { icon: "edit", className: "bg-neutral-50 text-neutral-700 ring-neutral-700/15", label: "aiStateManual" },
  reviewed: { icon: "checkCircle", className: "bg-success-50 text-success-700 ring-success-700/15", label: "aiStateReviewed" },
};

function StateChip({ state, extra }: { state: AiTextState; extra?: string }) {
  const t = useT(inspectionToolsMessages);
  if (state === "writing") {
    return <span className="inline-flex items-center gap-1.5 rounded-sm bg-review-50 px-2 py-0.5 text-caption font-medium text-review-700"><Spinner small />{t("aiStateWriting")}</span>;
  }
  const chip = CHIP[state];
  if (!chip) return null;
  return (
    <span className={cx("inline-flex items-center gap-1 rounded-sm px-2 py-0.5 text-caption font-medium ring-1 ring-inset", chip.className)}>
      <Icon name={chip.icon} className="h-3.5 w-3.5" />{t(chip.label)}{extra}
    </span>
  );
}

/**
 * Editable report text with its AI provenance: a small state chip, a subtle accent while the AI draft is untouched,
 * the AI tools underneath and — once edited — the original draft kept for comparison / restore.
 * The <label> holds only the field label, so tests and screen readers keep a stable accessible name.
 */
export function AiTextEditor({ label, hint, value, onChange, disabled, aiText, state, rows = 5, tools, confidence }: {
  label: string; hint?: string; value: string; onChange: (value: string) => void; disabled?: boolean;
  aiText: string | null; state: AiTextState; rows?: number; tools?: ReactNode; confidence?: number | null;
}) {
  const t = useT(inspectionToolsMessages);
  const id = useId();
  const extra = state === "draft" && confidence != null ? ` · ${t("confidence", { percent: Math.round(confidence * 100) })}` : undefined;
  return (
    <div>
      <div className="mb-1.5 flex flex-wrap items-center justify-between gap-2">
        <label htmlFor={id} className="text-label font-medium text-ink-2">{label}</label>
        <StateChip state={state} extra={extra} />
      </div>
      <Textarea id={id} rows={rows} disabled={disabled} value={value} onChange={(e) => onChange(e.target.value)}
        className={cx(state === "draft" && "border-review-500/40 bg-review-50/30")} />
      {hint && <p className="mt-1 text-caption text-ink-3">{hint}</p>}
      {(tools || (aiText && state === "edited")) && (
        <div className="mt-2 flex flex-wrap items-start gap-2">
          {tools}
          {aiText && state === "edited" && !disabled && (
            <details className="w-full rounded-md bg-surface-2 px-3 py-2 text-label ring-1 ring-inset ring-line">
              <summary className="cursor-pointer font-medium text-ink-2">{t("showAiDraft")}</summary>
              <p className="mt-2 whitespace-pre-line text-ink-2">{aiText}</p>
              <Button variant="ghost" size="sm" icon="refresh" className="mt-1" onClick={() => onChange(aiText)}>{t("restoreAiDraft")}</Button>
            </details>
          )}
        </div>
      )}
    </div>
  );
}
