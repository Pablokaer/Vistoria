"use client";

import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { useState } from "react";
import { useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";
import { cx } from "@/lib/cx";
import { Button } from "./Button";

/** Label + control + hint/error. The <label> wraps the control, so they are always associated. */
export function Field({ label, hint, error, children, className }: { label: string; hint?: string; error?: string; children: ReactNode; className?: string }) {
  return (
    <label className={cx("block", className)}>
      <span className="mb-1.5 block text-label font-medium text-ink-2">{label}</span>
      {children}
      {hint && !error && <span className="mt-1 block text-caption text-ink-3">{hint}</span>}
      {error && <span className="mt-1 block text-caption text-danger-700">{error}</span>}
    </label>
  );
}

// 16px text on phones prevents iOS zoom-on-focus; 14px from sm up.
const CONTROL = "block w-full rounded-md border border-line-strong bg-surface px-3 py-2 text-base text-ink shadow-card outline-none transition " +
  "placeholder:text-ink-4 hover:border-ink-4 focus:border-brand focus:ring-3 focus:ring-brand/15 disabled:bg-surface-2 disabled:text-ink-3 sm:text-body";

export const Input = (props: InputHTMLAttributes<HTMLInputElement>) => <input {...props} className={cx(CONTROL, "min-h-10", props.className)} />;
export const Textarea = (props: TextareaHTMLAttributes<HTMLTextAreaElement>) => <textarea {...props} className={cx(CONTROL, "leading-relaxed", props.className)} />;
export const Select = (props: SelectHTMLAttributes<HTMLSelectElement>) => <select {...props} className={cx(CONTROL, "min-h-10 pr-8", props.className)} />;

/** Read-only value with a copy button that confirms the copy. */
export function CopyField({ value, label }: { value: string; label: string }) {
  const t = useT(uiMessages);
  const [copied, setCopied] = useState(false);
  const copy = async () => {
    await navigator.clipboard?.writeText(value);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  };
  return (
    <Field label={label}>
      <div className="flex gap-2">
        <Input readOnly value={value} onFocus={(e) => e.currentTarget.select()} className="font-mono text-label" />
        <Button type="button" variant="secondary" icon={copied ? "check" : undefined} onClick={() => void copy()}>{copied ? t("copied") : t("copy")}</Button>
      </div>
    </Field>
  );
}

/** Accessible on/off switch (a checkbox underneath). Example: `<Switch checked={on} onChange={setOn} label="Defects found" />` */
export function Switch({ checked, onChange, label, description, disabled }:
  { checked: boolean; onChange: (checked: boolean) => void; label: string; description?: string; disabled?: boolean }) {
  return (
    <label className={cx("flex cursor-pointer items-start gap-3", disabled && "cursor-not-allowed opacity-60")}>
      <input type="checkbox" role="switch" className="peer sr-only" checked={checked} disabled={disabled} onChange={(e) => onChange(e.target.checked)} />
      <span aria-hidden="true" className="relative mt-0.5 h-5 w-9 shrink-0 rounded-full bg-line-strong transition peer-checked:bg-brand peer-focus-visible:ring-2 peer-focus-visible:ring-brand peer-focus-visible:ring-offset-2 after:absolute after:left-0.5 after:top-0.5 after:h-4 after:w-4 after:rounded-full after:bg-white after:shadow-card after:transition peer-checked:after:translate-x-4" />
      <span className="min-w-0">
        <span className="block text-body font-medium text-ink">{label}</span>
        {description && <span className="block text-caption text-ink-3">{description}</span>}
      </span>
    </label>
  );
}
