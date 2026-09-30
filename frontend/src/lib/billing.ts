// Billing API calls and price formatting. The browser never decides that a payment succeeded: it starts a
// checkout, is sent to the provider, and afterwards only *reads* the subscription status from the API.

import { get, post } from "./api";
import type { BillingPlan, CheckoutStart, CheckoutStatus, PlanInterval, SandboxPaymentResult, SandboxSession } from "./types";

export const DEFAULT_PLAN_CODE = "professional";

/** Target of every "Get started" CTA: company registration with the plan pre-selected. */
export const GET_STARTED_HREF = `/register?plan=${DEFAULT_PLAN_CODE}`;

/** Public list of paid plans (there is no free plan). Example: `const plans = await fetchPlans();` */
export const fetchPlans = () => get<BillingPlan[]>("/api/billing/plans");

/** Starts or resumes the checkout. Example: `window.location.assign((await startCheckout("professional")).checkoutUrl)` */
export const startCheckout = (planCode: string) => post<CheckoutStart>("/api/billing/checkout", { planCode });

export const fetchCheckoutStatus = (checkoutId: string) => get<CheckoutStatus>(`/api/billing/checkout/${encodeURIComponent(checkoutId)}`);

export const fetchSandboxSession = (sessionId: string) => get<SandboxSession>(`/api/billing/sandbox/sessions/${encodeURIComponent(sessionId)}`);

export const paySandboxSession = (sessionId: string, cardNumber: string) =>
  post<SandboxPaymentResult>(`/api/billing/sandbox/sessions/${encodeURIComponent(sessionId)}/pay`, { cardNumber });

/** Example: `formatPrice(4900, "EUR") // "€49"` — cents are shown only when the price has them. */
export function formatPrice(priceCents: number, currency: string): string {
  const whole = priceCents % 100 === 0;
  return new Intl.NumberFormat("en-IE", {
    style: "currency", currency, minimumFractionDigits: whole ? 0 : 2, maximumFractionDigits: 2,
  }).format(priceCents / 100);
}

const INTERVAL_LABEL: Record<PlanInterval, string> = { Month: "month", Year: "year" };

/** Example: `intervalLabel("Month") // "month"` */
export const intervalLabel = (interval: PlanInterval) => INTERVAL_LABEL[interval] ?? interval.toLowerCase();

/** Sends the browser to the provider's page; internal (sandbox) URLs keep the SPA, external ones leave it. */
export function goToCheckout(url: string, navigate: (path: string) => void) {
  const target = new URL(url, window.location.origin);
  // The sandbox page is part of this app even when WEB_BASE_URL names another host.
  const internal = target.origin === window.location.origin || target.pathname.startsWith("/checkout/sandbox/");
  if (internal) navigate(target.pathname + target.search);
  else window.location.assign(target.toString());
}
