"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Icon } from "@/components/landing/icons";
import { ErrorBanner, LinkButton, Spinner } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { billingMessages, type BillingKey } from "@/i18n/messages/billing";
import { errorMessage } from "@/lib/api";
import { homeFor, useAuth } from "@/lib/auth";
import { fetchCheckoutStatus } from "@/lib/billing";
import type { CheckoutStatus } from "@/lib/types";

const POLL_MS = 1500;
const GIVE_UP_AFTER_MS = 90_000;
const REDIRECT_AFTER_MS = 1800;

type Phase = "waiting" | "active" | "failed" | "slow";

/**
 * The provider sends the user here after paying. The query string is NOT trusted: the page only polls the API
 * until the provider's webhook has activated the subscription, then refreshes the session and goes to the app.
 */
function SuccessContent() {
  const params = useSearchParams();
  const checkoutId = params.get("checkout");
  const { user, loading, reload } = useAuth();
  const router = useRouter();
  const [phase, setPhase] = useState<Phase>("waiting");
  const [error, setError] = useState<string | null>(null);
  const t = useT(billingMessages);
  const signedIn = !!user;
  // Kept in a ref: `reload` changes identity whenever the user changes, which must not restart the polling loop.
  const reloadRef = useRef(reload);
  useEffect(() => { reloadRef.current = reload; }, [reload]);

  useEffect(() => {
    if (!loading && !user) router.replace(`/login?next=${encodeURIComponent(`/subscription/success?checkout=${checkoutId ?? ""}`)}`);
  }, [loading, user, router, checkoutId]);

  useEffect(() => {
    if (!signedIn || !checkoutId || phase !== "waiting") return;
    const started = Date.now();
    let timer: ReturnType<typeof setTimeout>;
    const poll = async () => {
      try {
        const next = decidePhase(await fetchCheckoutStatus(checkoutId), Date.now() - started);
        if (next === "active") await reloadRef.current();
        if (next !== "waiting") { setPhase(next); return; }
      } catch (e) {
        setError(errorMessage(e));
      }
      timer = setTimeout(poll, POLL_MS);
    };
    void poll();
    return () => clearTimeout(timer);
  }, [checkoutId, phase, signedIn]);

  useEffect(() => {
    if (phase !== "active" || !user?.subscription.hasAccess) return;
    const t = setTimeout(() => router.replace(homeFor(user)), REDIRECT_AFTER_MS);
    return () => clearTimeout(t);
  }, [phase, user, router]);

  return (
    <CheckoutLayout step={phase === "active" ? "done" : "payment"} title={t(TITLES[phase])}>
      <ErrorBanner message={checkoutId ? error : t("missingCheckout")} />
      <PhaseBody phase={phase} />
    </CheckoutLayout>
  );
}

const TITLES: Record<Phase, BillingKey> = {
  waiting: "confirmingTitle",
  active: "activeTitle",
  failed: "failedTitle",
  slow: "slowTitle",
};

function decidePhase(status: CheckoutStatus, elapsedMs: number): Phase {
  if (status.subscription.hasAccess) return "active";
  if (status.status === "Failed" || status.status === "Expired") return "failed";
  return elapsedMs > GIVE_UP_AFTER_MS ? "slow" : "waiting";
}

function PhaseBody({ phase }: { phase: Phase }) {
  const t = useT(billingMessages);
  if (phase === "waiting") {
    return <div className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white p-6 text-slate-600"><Spinner /> {t("waitingText")}</div>;
  }
  if (phase === "active") {
    return (
      <div className="rounded-2xl border border-emerald-200 bg-white p-6">
        <span className="grid h-12 w-12 place-items-center rounded-full bg-emerald-100 text-emerald-700"><Icon name="check" className="h-6 w-6" strokeWidth={2.5} /></span>
        <p className="mt-4 text-slate-700">{t("confirmedText")}</p>
      </div>
    );
  }
  const text = phase === "failed"
    ? t("failedText")
    : t("slowText");
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6">
      <p className="text-slate-700">{text}</p>
      <LinkButton href="/checkout" className="mt-4">{t("backToCheckout")}</LinkButton>
    </div>
  );
}

export default function SubscriptionSuccessPage() {
  return <Suspense><SuccessContent /></Suspense>;
}
