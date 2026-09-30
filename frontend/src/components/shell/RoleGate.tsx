"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { PageSkeleton } from "@/components/ui";
import { homeFor, needsSubscription, SUBSCRIPTION_REQUIRED_PATH, useAuth } from "@/lib/auth";
import type { Role } from "@/lib/types";

/** Client-side guard for UX only — every rule is enforced again by the API. */
export function RoleGate({ role, children, allowWithoutCompany }: { role: Role; children: React.ReactNode; allowWithoutCompany?: boolean }) {
  const { user, loading } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const allowed = !!user && user.roles.includes(role);
  const unpaid = needsSubscription(user);
  const needsWorkspace = role === "Company" && !!user && !user.company && !allowWithoutCompany;

  useEffect(() => {
    if (loading) return;
    if (!user) router.replace(`/login?next=${encodeURIComponent(pathname)}`);
    else if (!allowed) router.replace(homeFor(user));
    else if (unpaid) router.replace(SUBSCRIPTION_REQUIRED_PATH);
    else if (needsWorkspace) router.replace("/company/onboarding");
  }, [loading, user, allowed, unpaid, needsWorkspace, router, pathname]);

  if (loading || !allowed || unpaid || needsWorkspace) return <div className="mx-auto max-w-page px-4 py-8"><PageSkeleton /></div>;
  return <>{children}</>;
}
