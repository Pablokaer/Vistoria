"use client";

import type { ReactNode } from "react";
import { Brand } from "@/components/AppShell";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useT } from "@/i18n/I18nProvider";
import { billingMessages, type BillingKey } from "@/i18n/messages/billing";

export type CheckoutStep = "account" | "plan" | "payment" | "done";

const STEPS: { key: CheckoutStep; label: BillingKey }[] = [
  { key: "account", label: "stepAccount" },
  { key: "plan", label: "stepPlan" },
  { key: "payment", label: "stepPayment" },
  { key: "done", label: "stepDone" },
];

/** Progress indicator for Register → Plan → Payment → Done. */
export function CheckoutSteps({ current }: { current: CheckoutStep }) {
  const t = useT(billingMessages);
  const index = STEPS.findIndex((s) => s.key === current);
  return (
    <ol aria-label={t("progressLabel")} className="flex items-center gap-2 text-xs font-medium">
      {STEPS.map((s, i) => (
        <li key={s.key} className="flex items-center gap-2" aria-current={i === index ? "step" : undefined}>
          <span className={`grid h-6 w-6 place-items-center rounded-full ${i < index ? "bg-brand text-white" : i === index ? "border-2 border-brand text-brand" : "border border-line-strong text-ink-4"}`}>{i + 1}</span>
          <span className={i === index ? "text-ink" : "hidden text-ink-3 sm:inline"}>{t(s.label)}</span>
          {i < STEPS.length - 1 && <span className="h-px w-4 bg-line-strong sm:w-8" aria-hidden="true" />}
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
    <div className="min-h-screen bg-canvas">
      <header className="sticky top-0 z-30 border-b border-line bg-surface/95 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-4xl items-center justify-between gap-4 px-4">
          <Brand alwaysShowName />
          <div className="flex items-center gap-3">
            {step && <CheckoutSteps current={step} />}
            <LanguageSwitcher />
          </div>
        </div>
      </header>
      <main className={`mx-auto px-4 py-8 sm:py-12 ${wide ? "max-w-4xl" : "max-w-lg"}`}>
        <h1 className="text-page font-semibold tracking-tight text-ink sm:text-[1.75rem] sm:leading-9">{title}</h1>
        {subtitle && <p className="mt-2 text-body text-ink-3">{subtitle}</p>}
        <div className="mt-6">{children}</div>
      </main>
    </div>
  );
}
