"use client";

import Link from "next/link";
import { usePlans } from "@/components/billing/usePlans";
import { Icon } from "@/components/icons";
import { ErrorBanner, Loading } from "@/components/ui";
import { useLocale, useT } from "@/i18n/I18nProvider";
import { billingMessages } from "@/i18n/messages/billing";
import { landingSectionMessages } from "@/i18n/messages/landing-sections";
import { formatPrice, intervalLabel } from "@/lib/billing";
import { cx } from "@/lib/cx";
import type { BillingPlan } from "@/lib/types";
import { EYEBROW, LANDING_CONTAINER, PRIMARY_LINK, SECTION_TITLE } from "./landingStyles";

/**
 * Pricing on the landing page and /pricing. Plans come from the API (configuration is the single source of truth),
 * so a second plan needs no change here. Example: `<PricingSection />`
 */
export function PricingSection() {
  const { data: plans, error, reload } = usePlans();
  const t = useT(landingSectionMessages);
  return (
    <section id="pricing" aria-labelledby="pricing-title" className="scroll-mt-16 border-t border-rule bg-white py-20 sm:py-28">
      <div className={cx(LANDING_CONTAINER, "grid gap-12 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:gap-20")}>
        <div>
          <p className={EYEBROW}>{t("pricingEyebrow")}</p>
          <h2 id="pricing-title" className={cx(SECTION_TITLE, "mt-4")}>{t("pricingTitle")}</h2>
          <p className="mt-5 max-w-[28rem] text-[1.0625rem] leading-relaxed text-ink-2">{t("pricingIntro")}</p>
          <p className="mt-8 max-w-[28rem] border-t border-rule pt-4 text-sm text-paper-muted">{t("pricingPaymentNote")}</p>
        </div>
        <div>
          <ErrorBanner message={error} onRetry={() => void reload()} />
          {!plans && !error && <Loading label={t("pricingLoading")} />}
          {plans && <div className="grid gap-6">{plans.map((plan) => <LandingPlan key={plan.code} plan={plan} />)}</div>}
        </div>
      </div>
    </section>
  );
}

function LandingPlan({ plan }: { plan: BillingPlan }) {
  const { locale } = useLocale();
  const t = useT(landingSectionMessages);
  const billing = useT(billingMessages);
  return (
    <article data-testid="plan-card" className="rounded-md border border-rule-strong bg-paper">
      <div className="border-b border-rule px-6 py-6 sm:px-8">
        <h3 className="font-display text-[1.75rem] leading-none text-ink">{plan.name}</h3>
        <p className="mt-2 max-w-[28rem] text-sm text-paper-muted">{plan.description}</p>
        <p className="mt-6 flex flex-wrap items-baseline gap-x-2 gap-y-1">
          <span className="tabular text-[2.75rem] font-semibold leading-none tracking-tight text-ink">{formatPrice(plan.priceCents, plan.currency, locale)}</span>
          <span className="text-sm text-paper-muted">/ {intervalLabel(plan.interval, locale)}</span>
          <span className="w-full text-xs text-paper-muted">{billing(plan.interval === "Year" ? "billedYearly" : "billedMonthly")}</span>
        </p>
      </div>
      <ul className="grid gap-x-6 gap-y-3 px-6 py-6 sm:grid-cols-2 sm:px-8">
        {plan.features.map((f) => (
          <li key={f} className="flex gap-2.5 text-sm text-ink-2"><Icon name="check" className="mt-0.5 h-4 w-4 shrink-0 text-movein" strokeWidth={2.25} />{f}</li>
        ))}
      </ul>
      <div className="px-6 pb-6 sm:px-8">
        <Link href={`/register?plan=${encodeURIComponent(plan.code)}`} className={cx(PRIMARY_LINK, "w-full")}>
          {t("pricingGetStarted")}<Icon name="arrowRight" className="h-4 w-4" />
        </Link>
      </div>
    </article>
  );
}
