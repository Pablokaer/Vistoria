"use client";

import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { PlanCard } from "@/components/billing/PlanCard";
import { usePlans } from "@/components/billing/usePlans";
import { useSignedInForBilling } from "@/components/billing/useSignedInForBilling";
import { Button, ErrorBanner, Loading, Notice } from "@/components/ui";
import { errorMessage } from "@/lib/api";
import { DEFAULT_PLAN_CODE, formatPrice, goToCheckout, intervalLabel, startCheckout } from "@/lib/billing";
import type { BillingPlan } from "@/lib/types";

/** Plan selection + order summary. Continuing starts (or resumes) the checkout and leaves for the provider's page. */
function CheckoutContent() {
  const user = useSignedInForBilling();
  const params = useSearchParams();
  const router = useRouter();
  const { data: plans, error: plansError, reload } = usePlans();
  const [chosen, setChosen] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (!user) return <CheckoutLayout step="plan" title="Choose your plan"><Loading /></CheckoutLayout>;
  const plan = pickPlan(plans, chosen ?? params.get("plan"));

  async function continueToPayment() {
    if (!plan) return;
    setBusy(true);
    setError(null);
    try {
      goToCheckout((await startCheckout(plan.code)).checkoutUrl, (path) => router.push(path));
    } catch (e) {
      setError(errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <CheckoutLayout step="plan" wide title="Choose your plan" subtitle={<>Signed in as <strong className="font-medium text-ink">{user.email}</strong>. A subscription is required to use InspectFlow.</>}>
      {params.get("cancelled") && <Notice tone="warning">Payment was cancelled — nothing was charged. You can continue whenever you are ready.</Notice>}
      <ErrorBanner message={plansError} onRetry={() => void reload()} />
      <ErrorBanner message={error} />
      {!plans && !plansError && <Loading label="Loading plans…" />}
      {plans && plan && (
        <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
          <fieldset className="grid gap-4">
            <legend className="sr-only">Plan</legend>
            {plans.map((p) => (
              <label key={p.code} className="block cursor-pointer">
                <input type="radio" name="plan" className="sr-only" checked={p.code === plan.code} onChange={() => setChosen(p.code)} />
                <PlanCard plan={p} selected={p.code === plan.code} compact />
              </label>
            ))}
          </fieldset>
          <OrderSummary plan={plan} busy={busy} onContinue={() => void continueToPayment()} />
        </div>
      )}
    </CheckoutLayout>
  );
}

function pickPlan(plans: BillingPlan[] | null, code: string | null): BillingPlan | null {
  if (!plans || plans.length === 0) return null;
  return plans.find((p) => p.code === code) ?? plans.find((p) => p.code === DEFAULT_PLAN_CODE) ?? plans[0];
}

function OrderSummary({ plan, busy, onContinue }: { plan: BillingPlan; busy: boolean; onContinue: () => void }) {
  const price = formatPrice(plan.priceCents, plan.currency);
  return (
    <aside className="h-fit rounded-2xl border border-slate-200 bg-white p-5 shadow-sm lg:sticky lg:top-6">
      <h2 className="text-base font-semibold text-ink">Order summary</h2>
      <dl className="mt-4 space-y-2 text-sm">
        <div className="flex justify-between"><dt className="text-slate-600">{plan.name} plan</dt><dd className="font-medium">{price} / {intervalLabel(plan.interval)}</dd></div>
        <div className="flex justify-between border-t border-slate-100 pt-2"><dt className="font-medium text-ink">Due today</dt><dd className="font-semibold text-ink">{price}</dd></div>
      </dl>
      <Button size="lg" className="mt-5 w-full" loading={busy} onClick={onContinue}>Continue to payment</Button>
      <p className="mt-3 text-xs leading-relaxed text-slate-500">
        You will be taken to our payment provider. Your account is activated once the provider confirms the payment.
      </p>
    </aside>
  );
}

export default function CheckoutPage() {
  return <Suspense><CheckoutContent /></Suspense>;
}
