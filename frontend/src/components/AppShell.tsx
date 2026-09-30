"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { homeFor, needsSubscription, SUBSCRIPTION_REQUIRED_PATH, useAuth } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import type { Role } from "@/lib/types";
import { DevAccountSwitcher } from "./DevAccountSwitcher";
import { Loading } from "./ui";

const NAV: Record<Role, { href: string; label: string }[]> = {
  Company: [
    { href: "/company", label: "Dashboard" },
    { href: "/company/properties", label: "Properties" },
    { href: "/company/inspections/new", label: "New inspection" },
  ],
  Agent: [
    { href: "/agent", label: "Dashboard" },
    { href: "/agent/available", label: "Available" },
    { href: "/agent/inspections", label: "My inspections" },
    { href: "/agent/completed", label: "Completed" },
  ],
  Tenant: [{ href: "/tenant", label: "My inspections" }],
};

/** Logo + name. Inside the app it links to the user's home (`/app`); on public pages to the landing page. */
export function Brand({ href = "/", alwaysShowName }: { href?: string; alwaysShowName?: boolean }) {
  return (
    <Link href={href} className="flex items-center gap-2 font-semibold text-ink">
      <span className="grid h-8 w-8 place-items-center rounded-lg bg-brand text-sm font-bold text-white">IF</span>
      <span className={alwaysShowName ? "inline" : "hidden sm:inline"}>InspectFlow</span>
    </Link>
  );
}

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

  if (loading || !allowed || unpaid || needsWorkspace) return <div className="mx-auto max-w-6xl px-4"><Loading /></div>;
  return <>{children}</>;
}

export function AppShell({ role, children, wide }: { role: Role; children: React.ReactNode; wide?: boolean }) {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const links = role === "Company" && !user?.company ? [] : NAV[role];

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/95 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-6xl items-center gap-4 px-4">
          <Brand href="/app" />
          <nav className="-mx-1 flex flex-1 gap-1 overflow-x-auto">
            {links.map((l) => {
              const active = l.href === `/${role.toLowerCase()}` ? pathname === l.href : pathname.startsWith(l.href);
              return (
                <Link key={l.href} href={l.href}
                  className={`whitespace-nowrap rounded-md px-3 py-2 text-sm font-medium ${active ? "bg-brand-50 text-brand" : "text-slate-600 hover:text-slate-900"}`}>
                  {l.label}
                </Link>
              );
            })}
          </nav>
          <div className="flex items-center gap-3 text-sm">
            <DevAccountSwitcher />
            <span className="hidden text-right leading-tight md:block">
              <span className="block font-medium text-slate-800">{user?.fullName}</span>
              <span className="block text-xs text-slate-500">{user?.company?.companyName ?? role}</span>
            </span>
            <button onClick={() => void logout()} className="rounded-md px-2 py-1 text-slate-600 hover:bg-slate-100">Sign out</button>
          </div>
        </div>
      </header>
      <BillingWarning />
      <main className={`mx-auto px-4 py-6 ${wide ? "max-w-6xl" : "max-w-5xl"}`}>{children}</main>
    </div>
  );
}

/** A failed renewal keeps access during the grace period; say so before access ends. */
function BillingWarning() {
  const { user } = useAuth();
  if (user?.subscription.status !== "PastDue") return null;
  return (
    <div role="status" className="border-b border-amber-200 bg-amber-50">
      <div className="mx-auto max-w-6xl px-4 py-2 text-sm text-amber-900">
        Your last payment failed. Access continues until {formatDate(user.subscription.accessEndsAt)} — update your payment method with your
        payment provider to avoid interruption.
      </div>
    </div>
  );
}
