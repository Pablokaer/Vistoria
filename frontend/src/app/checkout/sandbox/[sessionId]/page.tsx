"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Button, ErrorBanner, Field, Input, LinkButton, Loading, Notice } from "@/components/ui";
import { errorMessage } from "@/lib/api";
import { useSignedInForBilling } from "@/components/billing/useSignedInForBilling";
import { formatPrice, intervalLabel, paySandboxSession } from "@/lib/billing";
import { useApi } from "@/lib/hooks";
import type { SandboxSession } from "@/lib/types";

const APPROVED_CARD = "4242 4242 4242 4242";

/**
 * Development stand-in for a hosted payment page (Stripe Checkout). Paying here makes the sandbox provider send a
 * signed webhook to the API; this page never activates anything itself. Not reachable when a real provider is active.
 */
export default function SandboxPaymentPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const user = useSignedInForBilling();
  const router = useRouter();
  const { data: session, error: loadError } = useApi<SandboxSession>(user ? `/api/billing/sandbox/sessions/${encodeURIComponent(sessionId)}` : null);
  const [card, setCard] = useState(APPROVED_CARD);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function pay(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const result = await paySandboxSession(sessionId, card);
      if (!result.approved || !result.redirectUrl) { setError(result.message); return; }
      const target = new URL(result.redirectUrl, window.location.origin);
      router.replace(target.pathname + target.search);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <CheckoutLayout step="payment" title="Payment" subtitle="Sandbox payment provider — no real money is charged.">
      <Notice tone="warning">
        <strong>Test mode.</strong> Use <code>{APPROVED_CARD}</code> to approve or <code>4000 0000 0000 0002</code> to simulate a declined card.
      </Notice>
      <ErrorBanner message={loadError} />
      {!session && !loadError && <Loading />}
      {session && session.status !== "Open" && (
        <div className="rounded-2xl border border-slate-200 bg-white p-6">
          <p className="text-slate-700">This payment session is {session.status.toLowerCase()}.</p>
          <LinkButton href="/checkout" className="mt-4">Back to checkout</LinkButton>
        </div>
      )}
      {session?.status === "Open" && (
        <form onSubmit={pay} className="space-y-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-baseline justify-between border-b border-slate-100 pb-4">
            <span className="text-sm text-slate-600">InspectFlow {session.planName}</span>
            <span className="text-lg font-semibold text-ink">{formatPrice(session.priceCents, session.currency)} <span className="text-sm font-normal text-slate-500">/ {intervalLabel(session.interval)}</span></span>
          </div>
          <Field label="Email"><Input value={session.customerEmail} readOnly /></Field>
          <Field label="Card number"><Input inputMode="numeric" autoComplete="off" required value={card} onChange={(e) => setCard(e.target.value)} /></Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Expiry"><Input value="12 / 34" readOnly /></Field>
            <Field label="CVC"><Input value="123" readOnly /></Field>
          </div>
          <ErrorBanner message={error} />
          <Button type="submit" size="lg" className="w-full" loading={busy}>Pay {formatPrice(session.priceCents, session.currency)}</Button>
        </form>
      )}
    </CheckoutLayout>
  );
}
