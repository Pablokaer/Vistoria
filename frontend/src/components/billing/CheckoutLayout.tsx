import type { ReactNode } from "react";
import { Brand } from "@/components/AppShell";

export type CheckoutStep = "account" | "plan" | "payment" | "done";

const STEPS: { key: CheckoutStep; label: string }[] = [
  { key: "account", label: "Account" },
  { key: "plan", label: "Plan" },
  { key: "payment", label: "Payment" },
  { key: "done", label: "Done" },
];

/** Progress indicator for Register → Plan → Payment → Done. */
export function CheckoutSteps({ current }: { current: CheckoutStep }) {
  const index = STEPS.findIndex((s) => s.key === current);
  return (
    <ol aria-label="Sign-up progress" className="flex items-center gap-2 text-xs font-medium">
      {STEPS.map((s, i) => (
        <li key={s.key} className="flex items-center gap-2" aria-current={i === index ? "step" : undefined}>
          <span className={`grid h-6 w-6 place-items-center rounded-full ${i < index ? "bg-brand text-white" : i === index ? "border-2 border-brand text-brand" : "border border-slate-300 text-slate-400"}`}>{i + 1}</span>
          <span className={i === index ? "text-ink" : "hidden text-slate-500 sm:inline"}>{s.label}</span>
          {i < STEPS.length - 1 && <span className="h-px w-4 bg-slate-300 sm:w-8" aria-hidden="true" />}
        </li>
      ))}
    </ol>
  );
}

/**
 * Focused, distraction-free frame for the sign-up and payment pages (mobile first).
 * Example: `<CheckoutLayout step="plan" title="Choose your plan">…</CheckoutLayout>`
 */
export function CheckoutLayout({ step, title, subtitle, children, wide }: { step?: CheckoutStep; title: string; subtitle?: ReactNode; children: ReactNode; wide?: boolean }) {
  return (
    <div className="min-h-screen bg-slate-50">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-16 max-w-4xl items-center justify-between gap-4 px-4">
          <Brand alwaysShowName />
          {step && <CheckoutSteps current={step} />}
        </div>
      </header>
      <main className={`mx-auto px-4 py-8 sm:py-12 ${wide ? "max-w-4xl" : "max-w-lg"}`}>
        <h1 className="text-2xl font-semibold tracking-tight text-ink sm:text-3xl">{title}</h1>
        {subtitle && <p className="mt-2 text-slate-600">{subtitle}</p>}
        <div className="mt-6">{children}</div>
      </main>
    </div>
  );
}
