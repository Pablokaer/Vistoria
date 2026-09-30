"use client";

import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { useSignedInForBilling } from "@/components/billing/useSignedInForBilling";
import { Loading } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { billingMessages, type BillingKey } from "@/i18n/messages/billing";
import { useAuth } from "@/lib/auth";
import type { SubscriptionStatus, SubscriptionSummary } from "@/lib/types";
import Link from "next/link";

const COPY: Record<SubscriptionStatus, { title: BillingKey; text: BillingKey | null; cta: BillingKey }> = {
  None: { title: "noneTitle", text: "noneText", cta: "noneCta" },
  Pending: { title: "pendingTitle", text: "pendingText", cta: "pendingCta" },
  PastDue: { title: "pastDueTitle", text: "pastDueText", cta: "renewCta" },
  Expired: { title: "expiredTitle", text: "expiredText", cta: "renewCta" },
  Cancelled: { title: "cancelledTitle", text: "cancelledText", cta: "cancelledCta" },
  Active: { title: "activeTitle", text: null, cta: "activeCta" },
};

/** Where signed-in users without a valid subscription land (also after an abandoned checkout). */
export default function SubscriptionRequiredPage() {
  const user = useSignedInForBilling();
  const { logout } = useAuth();
  const t = useT(billingMessages);
  if (!user) return <CheckoutLayout title={t("subscriptionTitle")}><Loading /></CheckoutLayout>;
  const copy = COPY[user.subscription.status] ?? COPY.None;
  return (
    <CheckoutLayout step="plan" title={t(copy.title)}>
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <p className="text-slate-700">{copy.text ? t(copy.text) : ""}</p>
        <StatusDetails summary={user.subscription} />
        <Link href={`/checkout${user.subscription.planCode ? `?plan=${encodeURIComponent(user.subscription.planCode)}` : ""}`}
          className="mt-6 flex min-h-12 w-full items-center justify-center rounded-lg bg-brand px-5 text-base font-semibold text-white shadow-sm hover:bg-brand-dark">
          {t(copy.cta)}
        </Link>
        <p className="mt-4 text-center text-sm text-slate-500">
          {t("signedInAsEmail", { email: user.email })} · <button type="button" onClick={() => void logout()} className="font-medium text-brand hover:underline">{t("signOut")}</button>
        </p>
      </div>
    </CheckoutLayout>
  );
}

function StatusDetails({ summary }: { summary: SubscriptionSummary }) {
  const t = useT(billingMessages);
  const { formatDate, humanize } = useFormatters();
  if (summary.status === "None") return null;
  return (
    <dl className="mt-4 grid grid-cols-2 gap-3 rounded-lg bg-slate-50 p-4 text-sm">
      <div><dt className="text-slate-500">{t("status")}</dt><dd className="font-medium text-ink" data-testid="subscription-status">{humanize(summary.status)}</dd></div>
      {summary.planName && <div><dt className="text-slate-500">{t("plan")}</dt><dd className="font-medium text-ink">{summary.planName}</dd></div>}
      {summary.accessEndsAt && <div><dt className="text-slate-500">{t("accessEnded")}</dt><dd className="font-medium text-ink">{formatDate(summary.accessEndsAt)}</dd></div>}
    </dl>
  );
}
