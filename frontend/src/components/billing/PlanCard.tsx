"use client";

import type { ReactNode } from "react";
import { Icon } from "@/components/landing/icons";
import { useLocale, useT } from "@/i18n/I18nProvider";
import { billingMessages } from "@/i18n/messages/billing";
import { formatPrice, intervalLabel } from "@/lib/billing";
import type { BillingPlan } from "@/lib/types";

/**
 * One plan: name, price, interval, features and an action slot (a link on the landing page, a radio at checkout).
 * Name, description and features come from the API's plan configuration and are shown as configured.
 * Example: `<PlanCard plan={plan} action={<Link href="/register">Get started</Link>} />`
 */
export function PlanCard({ plan, action, selected, compact }: { plan: BillingPlan; action?: ReactNode; selected?: boolean; compact?: boolean }) {
  const { locale } = useLocale();
  const t = useT(billingMessages);
  return (
    <article data-testid="plan-card" className={`flex h-full flex-col rounded-2xl border bg-white ${compact ? "p-5" : "p-7"} ${selected ? "border-brand ring-2 ring-brand/20" : "border-slate-200"} shadow-sm`}>
      <h3 className="text-lg font-semibold text-ink">{plan.name}</h3>
      <p className="mt-1 text-sm text-slate-600">{plan.description}</p>
      <p className="mt-5 flex items-baseline gap-1.5">
        <span className="text-4xl font-semibold tracking-tight text-ink">{formatPrice(plan.priceCents, plan.currency, locale)}</span>
        <span className="text-sm text-slate-500">/ {intervalLabel(plan.interval, locale)}</span>
      </p>
      <p className="mt-1 text-xs text-slate-500">{t(plan.interval === "Year" ? "billedYearly" : "billedMonthly")}</p>
      <ul className={`${compact ? "mt-4" : "mt-6"} flex-1 space-y-2.5`}>
        {plan.features.map((f) => (
          <li key={f} className="flex gap-2.5 text-sm text-slate-700"><Icon name="check" className="mt-0.5 h-4 w-4 shrink-0 text-brand" strokeWidth={2.5} />{f}</li>
        ))}
      </ul>
      {action && <div className="mt-7">{action}</div>}
    </article>
  );
}
