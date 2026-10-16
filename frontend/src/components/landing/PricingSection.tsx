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
import { LANDING_CONTAINER, PRIMARY_LINK, SECTION_SPACING } from "./landingStyles";
import { SectionHeading } from "./SectionHeading";

/**
 * Pricing on the landing page and /pricing. Plans come from the API (configuration is the single source of truth),
 * so prices are never written here and a second plan needs no change. Example: `<PricingSection />`
 */
export function PricingSection() {
  const { data: plans, error, reload } = usePlans();
  const t = useT(landingSectionMessages);
  return (
    <section id="pricing" aria-labelledby="pricing-title" className={cx("bg-site-mist", SECTION_SPACING)}>
      <div className={cx(LANDING_CONTAINER, "grid gap-12 lg:grid-cols-[minmax(0,0.85fr)_minmax(0,1.15fr)] lg:gap-20")}>
        <div>
          <SectionHeading id="pricing-title" eyebrow={t("pricingEyebrow")} title={t("pricingTitle")} intro={t("pricingIntro")} />
          <p className="mt-8 flex max-w-[30rem] gap-2.5 border-t border-site-line-strong pt-5 text-[0.875rem] leading-relaxed text-site-muted">
            <Icon name="lock" className="mt-0.5 h-4 w-4 shrink-0" />{t("pricingPaymentNote")}
          </p>
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
  const t = useT(landingSectionMessages);
  return (
    <article data-testid="plan-card" className="rounded-xl border border-site-line bg-white shadow-site-card">
      <PlanPrice plan={plan} />
      <ul className="grid gap-x-6 gap-y-3 px-6 py-6 sm:grid-cols-2 sm:px-8">
        {plan.features.map((f) => (
          <li key={f} className="flex gap-2.5 text-[0.875rem] text-site-ink"><Icon name="check" className="mt-0.5 h-4 w-4 shrink-0 text-site-blue" strokeWidth={2.25} />{f}</li>
        ))}
      </ul>
      <div className="px-6 pb-6 sm:px-8 sm:pb-8">
        <Link href={`/register?plan=${encodeURIComponent(plan.code)}`} className={cx(PRIMARY_LINK, "w-full")}>
          {t("pricingGetStarted")}<Icon name="arrowRight" className="h-4 w-4" />
        </Link>
      </div>
    </article>
  );
}

function PlanPrice({ plan }: { plan: BillingPlan }) {
  const { locale } = useLocale();
  const billing = useT(billingMessages);
  return (
    <div className="border-b border-site-line px-6 py-6 sm:px-8 sm:py-7">
      <h3 className="text-[1.25rem] font-semibold tracking-[-0.01em] text-site-ink">{plan.name}</h3>
      <p className="mt-1.5 max-w-[30rem] text-[0.875rem] text-site-muted">{plan.description}</p>
      <p className="mt-6 flex flex-wrap items-baseline gap-x-2 gap-y-1">
        <span className="tabular text-[2.75rem] font-semibold leading-none tracking-[-0.03em] text-site-ink">{formatPrice(plan.priceCents, plan.currency, locale)}</span>
        <span className="text-[0.875rem] text-site-muted">/ {intervalLabel(plan.interval, locale)}</span>
        <span className="w-full text-[0.75rem] text-site-muted">{billing(plan.interval === "Year" ? "billedYearly" : "billedMonthly")}</span>
      </p>
    </div>
  );
}
