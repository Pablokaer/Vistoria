"use client";

import { useApi } from "@/lib/hooks";
import type { BillingPlan } from "@/lib/types";

/** The paid plans from the API (single source of truth for names, prices and features). */
export function usePlans() {
  return useApi<BillingPlan[]>("/api/billing/plans");
}
