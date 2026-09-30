"use client";

import Link from "next/link";
import { PlanCard } from "@/components/billing/PlanCard";
import { usePlans } from "@/components/billing/usePlans";
import { ErrorBanner, Loading } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { landingDetailsMessages } from "@/i18n/messages/landingDetails";
import { SectionHeading } from "./SectionHeading";

/** Renders every configured plan; one plan is centred, more plans flow into a grid without layout changes. */
export function PricingSection() {
  const { data: plans, error, reload } = usePlans();
  const t = useT(landingDetailsMessages);
  const single = plans?.length === 1;
  return (
    <section id="pricing" aria-labelledby="pricing-title" className="scroll-mt-20 border-t border-line bg-white py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="pricing-title" eyebrow={t("pricingEyebrow")} title={t("pricingTitle")} centered intro={t("pricingIntro")} />
        <div className="mt-12">
          <ErrorBanner message={error} onRetry={() => void reload()} />
          {!plans && !error && <div className="flex justify-center"><Loading label={t("pricingLoading")} /></div>}
          {plans && (
            <div className={single ? "mx-auto max-w-md" : "grid gap-6 md:grid-cols-2 lg:grid-cols-3"}>
              {plans.map((plan) => (
                <PlanCard key={plan.code} plan={plan} action={
                  <Link href={`/register?plan=${encodeURIComponent(plan.code)}`}
                    className="flex min-h-12 w-full items-center justify-center rounded-lg bg-brand px-5 text-base font-semibold text-white shadow-card hover:bg-brand-dark">
                    {t("pricingGetStarted")}
                  </Link>
                } />
              ))}
            </div>
          )}
          <p className="mt-6 text-center text-sm text-ink-3">{t("pricingPaymentNote")}</p>
        </div>
      </div>
    </section>
  );
}
