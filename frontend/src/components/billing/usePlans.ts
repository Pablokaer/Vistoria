"use client";

import { useLocale } from "@/i18n/I18nProvider";
import { useApi } from "@/lib/hooks";
import type { BillingPlan } from "@/lib/types";

/**
 * The paid plans from the API (single source of truth for names, prices and features). The API answers in the
 * Accept-Language of the request; `lang` only changes the cache key so switching language fetches the texts again.
 */
export function usePlans() {
  const { locale } = useLocale();
  return useApi<BillingPlan[]>(`/api/billing/plans?lang=${locale}`);
}
