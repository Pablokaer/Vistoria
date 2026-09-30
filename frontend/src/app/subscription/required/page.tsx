"use client";

import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { useSignedInForBilling } from "@/components/billing/useSignedInForBilling";
import { Loading } from "@/components/ui";
import { useAuth } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import type { SubscriptionStatus, SubscriptionSummary } from "@/lib/types";
import Link from "next/link";

const COPY: Record<SubscriptionStatus, { title: string; text: string; cta: string }> = {
  None: { title: "Choose a plan to get started", text: "Your account is ready. InspectFlow has no free plan, so a subscription is needed before you can use the platform.", cta: "Choose a plan" },
  Pending: { title: "Finish setting up your subscription", text: "Your account was created but the payment was not completed. Continue where you left off — you do not need to register again.", cta: "Continue to payment" },
  PastDue: { title: "Your payment needs attention", text: "The last renewal could not be charged and the grace period has ended.", cta: "Renew subscription" },
  Expired: { title: "Your subscription has expired", text: "The paid period has ended. Renew to regain access to your properties, inspections and reports.", cta: "Renew subscription" },
  Cancelled: { title: "Your subscription was cancelled", text: "Subscribe again to regain access. Your workspace and data are kept.", cta: "Subscribe again" },
  Active: { title: "Your subscription is active", text: "", cta: "Open InspectFlow" },
};

/** Where signed-in users without a valid subscription land (also after an abandoned checkout). */
export default function SubscriptionRequiredPage() {
  const user = useSignedInForBilling();
  const { logout } = useAuth();
  if (!user) return <CheckoutLayout title="Subscription"><Loading /></CheckoutLayout>;
  const copy = COPY[user.subscription.status] ?? COPY.None;
  return (
    <CheckoutLayout step="plan" title={copy.title}>
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-slate-700">{copy.text}</p>
        <StatusDetails summary={user.subscription} />
        <Link href={`/checkout${user.subscription.planCode ? `?plan=${encodeURIComponent(user.subscription.planCode)}` : ""}`}
          className="mt-6 flex min-h-12 w-full items-center justify-center rounded-lg bg-brand px-5 text-base font-semibold text-white shadow-sm hover:bg-brand-dark">
          {copy.cta}
        </Link>
        <p className="mt-4 text-center text-sm text-slate-500">
          Signed in as {user.email} · <button type="button" onClick={() => void logout()} className="font-medium text-brand hover:underline">Sign out</button>
        </p>
      </div>
    </CheckoutLayout>
  );
}

function StatusDetails({ summary }: { summary: SubscriptionSummary }) {
  if (summary.status === "None") return null;
  return (
    <dl className="mt-4 grid grid-cols-2 gap-3 rounded-lg bg-slate-50 p-4 text-sm">
      <div><dt className="text-slate-500">Status</dt><dd className="font-medium text-ink" data-testid="subscription-status">{summary.status}</dd></div>
      {summary.planName && <div><dt className="text-slate-500">Plan</dt><dd className="font-medium text-ink">{summary.planName}</dd></div>}
      {summary.accessEndsAt && <div><dt className="text-slate-500">Access ended</dt><dd className="font-medium text-ink">{formatDate(summary.accessEndsAt)}</dd></div>}
    </dl>
  );
}
