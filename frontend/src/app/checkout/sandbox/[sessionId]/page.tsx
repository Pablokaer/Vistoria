"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Button, ErrorBanner, Field, Input, LinkButton, Loading, Notice } from "@/components/ui";
import { useFormatters, useLocale, useT } from "@/i18n/I18nProvider";
import { billingMessages } from "@/i18n/messages/billing";
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
  const { locale } = useLocale();
  const { humanize } = useFormatters();
  const t = useT(billingMessages);

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
    <CheckoutLayout step="payment" title={t("paymentTitle")} subtitle={t("sandboxSubtitle")}>
      <Notice tone="warning">
        <strong>{t("testMode")}</strong> {t("testModeUse")} <code>{APPROVED_CARD}</code> {t("testModeApprove")} <code>4000 0000 0000 0002</code> {t("testModeDecline")}
      </Notice>
      <ErrorBanner message={loadError} />
      {!session && !loadError && <Loading />}
      {session && session.status !== "Open" && (
        <div className="rounded-lg border border-line bg-surface p-6">
          <p className="text-ink-2">{t("sessionNotOpen", { status: humanize(session.status).toLowerCase() })}</p>
          <LinkButton href="/checkout" className="mt-4">{t("backToCheckout")}</LinkButton>
        </div>
      )}
      {session?.status === "Open" && (
        <form onSubmit={pay} className="space-y-5 rounded-lg border border-line bg-surface p-6 shadow-card">
          <div className="flex items-baseline justify-between border-b border-line pb-4">
            <span className="text-sm text-ink-3">InspectFlow {session.planName}</span>
            <span className="text-lg font-semibold text-ink">{formatPrice(session.priceCents, session.currency, locale)} <span className="text-sm font-normal text-ink-3">/ {intervalLabel(session.interval, locale)}</span></span>
          </div>
          <Field label={t("email")}><Input value={session.customerEmail} readOnly /></Field>
          <Field label={t("cardNumber")}><Input inputMode="numeric" autoComplete="off" required value={card} onChange={(e) => setCard(e.target.value)} /></Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label={t("expiry")}><Input value="12 / 34" readOnly /></Field>
            <Field label={t("cvc")}><Input value="123" readOnly /></Field>
          </div>
          <ErrorBanner message={error} />
          <Button type="submit" size="lg" className="w-full" loading={busy}>{t("pay", { amount: formatPrice(session.priceCents, session.currency, locale) })}</Button>
        </form>
      )}
    </CheckoutLayout>
  );
}
