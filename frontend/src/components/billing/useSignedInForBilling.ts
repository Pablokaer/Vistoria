"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { homeFor, needsSubscription, useAuth } from "@/lib/auth";

/**
 * Guard for the billing pages: visitors go to sign in (and come back), users who already have access go home.
 * Returns the user once it is allowed to stay. Example: `const user = useSignedInForBilling();`
 */
export function useSignedInForBilling() {
  const { user, loading } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const allowed = !!user && needsSubscription(user);

  useEffect(() => {
    if (loading) return;
    if (!user) router.replace(`/login?next=${encodeURIComponent(pathname + window.location.search)}`);
    else if (!needsSubscription(user)) router.replace(homeFor(user));
  }, [loading, user, router, pathname]);

  return allowed ? user : null;
}
