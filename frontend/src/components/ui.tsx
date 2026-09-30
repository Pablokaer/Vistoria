"use client";

// Client module: several primitives (Loading, Badge, CopyField, ...) read the UI language from I18nProvider.
// Server components may still render them; they only pass serializable props.
import Link from "next/link";
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";

const cx = (...c: (string | false | null | undefined)[]) => c.filter(Boolean).join(" ");

type Variant = "primary" | "secondary" | "danger" | "ghost";
const variants: Record<Variant, string> = {
  primary: "bg-brand text-white hover:bg-brand-dark disabled:bg-slate-300",
  secondary: "bg-white text-slate-800 border border-slate-300 hover:bg-slate-50 disabled:text-slate-400",
  danger: "bg-white text-red-700 border border-red-300 hover:bg-red-50",
  ghost: "text-brand hover:bg-brand-50",
};

export function Button({ variant = "primary", size = "md", className, loading, children, ...props }:
  ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant; size?: "md" | "lg"; loading?: boolean }) {
  return (
    <button
      {...props}
      disabled={props.disabled || loading}
      className={cx(
        "inline-flex items-center justify-center gap-2 rounded-lg font-medium transition disabled:cursor-not-allowed",
        size === "lg" ? "min-h-12 px-5 text-base" : "min-h-10 px-4 text-sm",
        variants[variant], className)}
    >
      {loading && <Spinner small />}
      {children}
    </button>
  );
}

export function LinkButton({ href, variant = "primary", className, children }: { href: string; variant?: Variant; className?: string; children: ReactNode }) {
  return (
    <Link href={href} className={cx("inline-flex min-h-10 items-center justify-center gap-2 rounded-lg px-4 text-sm font-medium transition", variants[variant], className)}>
      {children}
    </Link>
  );
}

export function Card({ title, actions, children, className }: { title?: ReactNode; actions?: ReactNode; children: ReactNode; className?: string }) {
  return (
    <section className={cx("rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-5", className)}>
      {(title || actions) && (
        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
          {title && <h2 className="text-base font-semibold text-slate-900">{title}</h2>}
          {actions}
        </div>
      )}
      {children}
    </section>
  );
}

export function PageHeader({ title, subtitle, actions, back }: { title: ReactNode; subtitle?: ReactNode; actions?: ReactNode; back?: { href: string; label: string } }) {
  return (
    <div className="mb-5">
      {back && <Link href={back.href} className="mb-2 inline-block text-sm text-brand hover:underline">← {back.label}</Link>}
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
          {subtitle && <p className="mt-1 text-sm text-slate-600">{subtitle}</p>}
        </div>
        {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
      </div>
    </div>
  );
}

export function Field({ label, hint, error, children }: { label: string; hint?: string; error?: string; children: ReactNode }) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-slate-700">{label}</span>
      {children}
      {hint && !error && <span className="mt-1 block text-xs text-slate-500">{hint}</span>}
      {error && <span className="mt-1 block text-xs text-red-600">{error}</span>}
    </label>
  );
}

const inputClass = "block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-base text-slate-900 shadow-sm outline-none focus:border-brand focus:ring-2 focus:ring-brand/20 sm:text-sm";
export const Input = (props: InputHTMLAttributes<HTMLInputElement>) => <input {...props} className={cx(inputClass, "min-h-10", props.className)} />;
export const Textarea = (props: TextareaHTMLAttributes<HTMLTextAreaElement>) => <textarea {...props} className={cx(inputClass, props.className)} />;
export const Select = (props: SelectHTMLAttributes<HTMLSelectElement>) => <select {...props} className={cx(inputClass, "min-h-10", props.className)} />;

export function Spinner({ small }: { small?: boolean }) {
  const t = useT(uiMessages);
  return <span className={cx("inline-block animate-spin rounded-full border-2 border-current border-t-transparent", small ? "h-4 w-4" : "h-6 w-6")} aria-label={t("loading")} />;
}

export function Loading({ label }: { label?: string }) {
  const t = useT(uiMessages);
  return <div className="flex items-center gap-3 py-10 text-slate-500"><Spinner /> {label ?? t("loading")}</div>;
}

export function ErrorBanner({ message, onRetry }: { message: string | null | undefined; onRetry?: () => void }) {
  const t = useT(uiMessages);
  if (!message) return null;
  return (
    <div role="alert" className="mb-4 flex flex-wrap items-center justify-between gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
      <span>{message}</span>
      {onRetry && <button onClick={onRetry} className="font-medium underline">{t("retry")}</button>}
    </div>
  );
}

export function Notice({ tone = "info", children }: { tone?: "info" | "success" | "warning"; children: ReactNode }) {
  const tones = { info: "border-sky-200 bg-sky-50 text-sky-900", success: "border-emerald-200 bg-emerald-50 text-emerald-900", warning: "border-amber-200 bg-amber-50 text-amber-900" };
  return <div className={cx("mb-4 rounded-lg border px-4 py-3 text-sm", tones[tone])}>{children}</div>;
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="rounded-xl border border-dashed border-slate-300 bg-white px-6 py-10 text-center">
      <p className="font-medium text-slate-800">{title}</p>
      {children && <div className="mt-2 text-sm text-slate-600">{children}</div>}
    </div>
  );
}

const statusColors: Record<string, string> = {
  Draft: "bg-slate-100 text-slate-700", Open: "bg-sky-100 text-sky-800", Assigned: "bg-indigo-100 text-indigo-800",
  InProgress: "bg-amber-100 text-amber-800", Review: "bg-violet-100 text-violet-800", Completed: "bg-emerald-100 text-emerald-800",
  AwaitingTenant: "bg-orange-100 text-orange-800", Accepted: "bg-emerald-100 text-emerald-800", Disputed: "bg-red-100 text-red-800",
  Cancelled: "bg-slate-100 text-slate-500", Expired: "bg-slate-100 text-slate-500", Pending: "bg-slate-100 text-slate-700",
  NewDamage: "bg-red-100 text-red-800", PreExisting: "bg-slate-100 text-slate-700", NormalWear: "bg-amber-100 text-amber-800",
  Unchanged: "bg-emerald-100 text-emerald-800", Resolved: "bg-sky-100 text-sky-800", UnableToDetermine: "bg-slate-100 text-slate-700",
};

export function Badge({ value, className }: { value: string; className?: string }) {
  const { humanize } = useFormatters();
  return <span className={cx("inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium", statusColors[value] ?? "bg-slate-100 text-slate-700", className)}>{humanize(value)}</span>;
}

export function ProgressBar({ value, total, label }: { value: number; total: number; label?: string }) {
  const t = useT(uiMessages);
  const pct = total === 0 ? 0 : Math.round((value / total) * 100);
  return (
    <div>
      <div className="mb-1 flex justify-between text-xs text-slate-600"><span>{label ?? t("roomsCompleted", { value, total })}</span><span>{pct}%</span></div>
      <div className="h-2 overflow-hidden rounded-full bg-slate-200"><div className="h-full rounded-full bg-brand transition-all" style={{ width: `${pct}%` }} /></div>
    </div>
  );
}

export function Stat({ label, value, href }: { label: string; value: number | string; href?: string }) {
  const inner = (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition hover:border-brand/40">
      <div className="text-2xl font-semibold text-slate-900">{value}</div>
      <div className="text-sm text-slate-600">{label}</div>
    </div>
  );
  return href ? <Link href={href}>{inner}</Link> : inner;
}

export function DefinitionList({ items }: { items: [string, ReactNode][] }) {
  return (
    <dl className="grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-[max-content_1fr]">
      {items.map(([k, v]) => (
        <div key={k} className="contents">
          <dt className="text-slate-500">{k}</dt>
          <dd className="font-medium text-slate-900">{v}</dd>
        </div>
      ))}
    </dl>
  );
}

export function CopyField({ value, label }: { value: string; label: string }) {
  const t = useT(uiMessages);
  return (
    <Field label={label}>
      <div className="flex gap-2">
        <Input readOnly value={value} onFocus={(e) => e.currentTarget.select()} />
        <Button type="button" variant="secondary" onClick={() => navigator.clipboard?.writeText(value)}>{t("copy")}</Button>
      </div>
    </Field>
  );
}
