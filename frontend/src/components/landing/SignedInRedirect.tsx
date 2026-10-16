"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { BrandMark } from "@/components/AppShell";
import { Spinner } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { landingMessages } from "@/i18n/messages/landing";
import { homeFor, useAuth } from "@/lib/auth";

/**
 * Root route gate. Anonymous visitors (no session hint) get `children` — the landing page, already rendered on the
 * server. With a hint, nothing of the landing page is shown until the session is checked: a signed-in user is sent
 * to their home (`homeFor`: dashboard, onboarding or subscription page), a stale hint falls back to the landing page.
 * Example: `<SignedInRedirect sessionHint={await hasSessionHint()}><Landing /></SignedInRedirect>`
 */
export function SignedInRedirect({ sessionHint, children }: { sessionHint: boolean; children: React.ReactNode }) {
  const { user, loading } = useAuth();
  const router = useRouter();
  useEffect(() => {
    if (!loading && user) router.replace(homeFor(user));
  }, [loading, user, router]);
  if (user || (sessionHint && loading)) return <OpeningDashboard />;
  return children;
}

function OpeningDashboard() {
  const t = useT(landingMessages);
  return (
    <div className="grid min-h-screen place-items-center bg-paper px-4" data-testid="opening-dashboard">
      <div className="flex items-center gap-3 text-sm text-paper-muted"><BrandMark /><Spinner small /><span>{t("resolvingSession")}</span></div>
    </div>
  );
}
