"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState, type Ref } from "react";
import { Brand } from "@/components/AppShell";
import { Icon } from "@/components/icons";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useT } from "@/i18n/I18nProvider";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { useAuth } from "@/lib/auth";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { LANDING_CONTAINER } from "./landingStyles";
import { useScrollFlag } from "./useScrollFlag";

const NAV: { href: string; label: LandingKey }[] = [
  { href: "/#features", label: "navFeatures" },
  { href: "/#how-it-works", label: "navHowItWorks" },
  { href: "/#agents", label: "navAgents" },
  { href: "/#pricing", label: "navPricing" },
];

/**
 * Open state of the mobile menu. Escape closes it, as keyboard users expect from a disclosure, and hands focus back
 * to the toggle: the menu (and the link that may hold focus) unmounts, which would otherwise drop focus on <body>.
 */
function useMobileMenu() {
  const [open, setOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const close = useCallback(() => setOpen(false), []);
  const toggle = useCallback(() => setOpen((v) => !v), []);
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== "Escape") return;
      setOpen(false);
      buttonRef.current?.focus();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);
  return { open, close, toggle, buttonRef };
}

/**
 * Public header: section links, language, Sign in and "Get started". Sticky; gains a hairline and a soft shadow once
 * the page scrolls. Collapses into an accessible menu below 1024px.
 */
export function LandingHeader() {
  const t = useT(landingMessages);
  const headerRef = useScrollFlag<HTMLElement>();
  const { open, close, toggle, buttonRef } = useMobileMenu();
  return (
    <header ref={headerRef} className="group/header sticky top-0 z-40 border-b border-transparent bg-white transition-[border-color,box-shadow] duration-300 data-[scrolled=true]:border-site-line data-[scrolled=true]:shadow-[0_6px_20px_-12px_rgb(11_23_40/0.18)]">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-3 focus:z-50 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:text-sm focus:font-medium focus:text-site-ink">
        {t("skipToContent")}
      </a>
      <div className={cx(LANDING_CONTAINER, "flex h-[4.5rem] items-center justify-between gap-4 transition-[height] duration-300 group-data-[scrolled=true]/header:h-16")}>
        <Brand alwaysShowName />
        <nav aria-label={t("navMain")} className="hidden items-center gap-1 lg:flex">
          {NAV.map((l) => (
            <Link key={l.href} href={l.href} className="rounded-md px-3 py-2 text-[0.875rem] font-medium text-site-muted transition hover:text-site-ink">{t(l.label)}</Link>
          ))}
        </nav>
        <div className="hidden items-center gap-2 lg:flex"><LanguageSwitcher /><AccountActions /></div>
        <MenuButton ref={buttonRef} open={open} onToggle={toggle} />
      </div>
      {open && <MobileMenu onNavigate={close} />}
    </header>
  );
}

function MenuButton({ ref, open, onToggle }: { ref: Ref<HTMLButtonElement>; open: boolean; onToggle: () => void }) {
  const t = useT(landingMessages);
  return (
    <button ref={ref} type="button" onClick={onToggle} aria-expanded={open} aria-controls="mobile-menu" aria-label={open ? t("closeMenu") : t("openMenu")}
      className="grid h-11 w-11 place-items-center rounded-lg text-site-ink transition hover:bg-site-mist lg:hidden">
      <Icon name={open ? "close" : "menu"} className="h-6 w-6" />
    </button>
  );
}

/** Below 1024px: the same links as full-width rows (48px touch targets), then language and account actions. */
function MobileMenu({ onNavigate }: { onNavigate: () => void }) {
  const t = useT(landingMessages);
  return (
    <div id="mobile-menu" className="max-h-[calc(100svh-4.5rem)] overflow-y-auto border-t border-site-line bg-white px-4 pb-6 pt-2 shadow-site-card sm:px-6 lg:hidden">
      <nav aria-label={t("navMobile")} className="flex flex-col">
        {NAV.map((l) => (
          <Link key={l.href} href={l.href} onClick={onNavigate} className="flex min-h-12 items-center border-b border-site-line text-base font-medium text-site-ink">{t(l.label)}</Link>
        ))}
      </nav>
      <div className="mt-5 grid gap-2"><LanguageSwitcher className="mb-2 justify-self-start" /><AccountActions stacked onNavigate={onNavigate} /></div>
    </div>
  );
}

/** Signed-in visitors of /pricing get "Open dashboard"; on "/" they are redirected before this renders. */
function AccountActions({ stacked, onNavigate }: { stacked?: boolean; onNavigate?: () => void }) {
  const { user, loading } = useAuth();
  const t = useT(landingMessages);
  const size = stacked ? "grid min-h-12 place-items-center" : "py-2";
  const secondary = cx("rounded-lg px-4 text-[0.875rem] font-semibold text-site-ink transition hover:bg-site-mist", stacked && "border border-site-line-strong", size);
  const primary = cx("rounded-lg bg-site-blue px-4 text-[0.875rem] font-semibold text-white transition hover:bg-site-blue-dark", size);
  if (!loading && user) return <Link href="/app" onClick={onNavigate} className={primary}>{t("openDashboard")}</Link>;
  return (
    <>
      <Link href="/login" onClick={onNavigate} className={secondary}>{t("signIn")}</Link>
      <Link href={GET_STARTED_HREF} onClick={onNavigate} className={primary}>{t("getStarted")}</Link>
    </>
  );
}
