"use client";

import { useFormatters, useT } from "@/i18n/I18nProvider";
import { shellMessages } from "@/i18n/messages/shell";
import { useAuth } from "@/lib/auth";
import { cx } from "@/lib/cx";
import type { Role } from "@/lib/types";
import { MobileTabBar, MobileTopBar } from "./MobileNav";
import { SidebarContent } from "./Sidebar";

/**
 * Signed-in layout: fixed navy sidebar on desktop (lg+), top bar + drawer + bottom tabs on phones/tablets.
 * The page itself owns its header (PageHeader) so the top of each page carries context, not navigation.
 */
export function AppShell({ role, children, wide }: { role: Role; children: React.ReactNode; wide?: boolean }) {
  return (
    <div className="min-h-screen">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-sidebar lg:block"><SidebarContent role={role} /></aside>
      <MobileTopBar role={role} />
      <div className="lg:pl-sidebar">
        <BillingWarning />
        <main className={cx("mx-auto px-4 pb-24 pt-5 sm:px-6 lg:px-8 lg:pb-12 lg:pt-7", wide ? "max-w-page" : "max-w-[64rem]")}>{children}</main>
      </div>
      <MobileTabBar role={role} />
    </div>
  );
}

/** A failed renewal keeps access during the grace period; say so before access ends. */
function BillingWarning() {
  const { user } = useAuth();
  const t = useT(shellMessages);
  const { formatDate } = useFormatters();
  if (user?.subscription.status !== "PastDue") return null;
  return (
    <div role="status" className="border-b border-warning-500/30 bg-warning-50 px-4 py-2.5 text-label text-warning-700 sm:px-8">
      {t("pastDueWarning", { date: formatDate(user.subscription.accessEndsAt) })}
    </div>
  );
}

/** Time-of-day greeting for page headers. Example: `useGreeting()` → "Good morning, Alex". */
export function useGreeting(): string {
  const t = useT(shellMessages);
  const { user } = useAuth();
  const firstName = user?.fullName.split(/\s+/)[0] ?? "";
  const hour = new Date().getHours();
  const key = hour < 12 ? "greetingMorning" : hour < 18 ? "greetingAfternoon" : "greetingEvening";
  return t(key, { name: firstName });
}
