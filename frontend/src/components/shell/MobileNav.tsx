"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { Icon } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { shellMessages } from "@/i18n/messages/shell";
import { useAuth } from "@/lib/auth";
import { cx } from "@/lib/cx";
import type { Role } from "@/lib/types";
import { Brand } from "./Brand";
import { isActive, navigationFor } from "./navigation";
import { NotificationsMenu } from "./NotificationsMenu";
import { SidebarContent } from "./Sidebar";

/** Phone/tablet top bar: menu (drawer with the full sidebar), brand, notifications. */
export function MobileTopBar({ role }: { role: Role }) {
  const t = useT(shellMessages);
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  // Close the drawer on navigation (e.g. browser back).
  // eslint-disable-next-line react-hooks/set-state-in-effect -- syncing UI state to the route is the intent.
  useEffect(() => { setOpen(false); }, [pathname]);
  return (
    <>
      <header className="sticky top-0 z-30 flex h-14 items-center gap-2 border-b border-line bg-surface/95 px-2 backdrop-blur lg:hidden">
        <button type="button" onClick={() => setOpen(true)} aria-label={t("openMenu")} aria-expanded={open}
          className="grid h-10 w-10 place-items-center rounded-md text-ink-2 hover:bg-neutral-50"><Icon name="menu" className="h-5 w-5" /></button>
        <Brand href="/app" alwaysShowName />
        <div className="ml-auto"><NotificationsMenu /></div>
      </header>
      {open && (
        <div className="fixed inset-0 z-50 lg:hidden" role="dialog" aria-modal="true" aria-label={t("mainNavigation")}>
          <button type="button" aria-label={t("closeMenu")} onClick={() => setOpen(false)} className="absolute inset-0 bg-ink/40 animate-fade-in" />
          <div className="relative h-full w-72 max-w-[85vw] shadow-overlay animate-slide-in">
            <SidebarContent role={role} onNavigate={() => setOpen(false)} />
          </div>
        </div>
      )}
    </>
  );
}

// Inspection execution pages have their own sticky action bar; a second bar would crowd the phone screen.
const FOCUS_ROUTE = /^\/agent\/inspections\/[^/]+\/(capture|descriptions|review)$/;
const isFocusRoute = (pathname: string) => FOCUS_ROUTE.test(pathname);

/**
 * Thumb-reachable tab bar for the role's main destinations (phones only). Hidden when the role has a single
 * destination (tenants), where a tab bar would add nothing.
 */
export function MobileTabBar({ role }: { role: Role }) {
  const t = useT(shellMessages);
  const { user } = useAuth();
  const pathname = usePathname();
  const items = navigationFor(role, !!user?.company).groups.flatMap((g) => g.items);
  if (items.length < 2 || isFocusRoute(pathname)) return null;
  return (
    <nav aria-label={t("mainNavigation")} className="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-surface/95 pb-[env(safe-area-inset-bottom)] backdrop-blur lg:hidden">
      <div className="grid" style={{ gridTemplateColumns: `repeat(${items.length}, minmax(0, 1fr))` }}>
        {items.map((item) => {
          const active = isActive(item.href, pathname, role);
          return (
            <Link key={item.href} href={item.href} aria-current={active ? "page" : undefined}
              className={cx("flex flex-col items-center gap-0.5 px-1 pb-1.5 pt-2 text-[11px] font-medium", active ? "text-brand" : "text-ink-3")}>
              <Icon name={item.icon} className="h-5 w-5" />
              <span className="max-w-full truncate">{t(item.label)}</span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
