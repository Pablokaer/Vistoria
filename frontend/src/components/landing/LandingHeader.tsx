"use client";

import Link from "next/link";
import { useState } from "react";
import { Brand } from "@/components/AppShell";
import { Icon } from "@/components/icons";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useT } from "@/i18n/I18nProvider";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { useAuth } from "@/lib/auth";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { LANDING_CONTAINER } from "./landingStyles";

const NAV: { href: string; label: LandingKey }[] = [
  { href: "/#workflow", label: "navWorkflow" },
  { href: "/#report", label: "navReport" },
  { href: "/#agencies", label: "navAgencies" },
  { href: "/#pricing", label: "navPricing" },
];

/** Public header: section links, language, Sign in and "Get started"; collapses into a menu on phones. */
export function LandingHeader() {
  const [open, setOpen] = useState(false);
  const t = useT(landingMessages);
  const close = () => setOpen(false);
  return (
    <header className="sticky top-0 z-30 border-b border-rule bg-paper">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-3 focus:z-40 focus:rounded-md focus:bg-white focus:px-3 focus:py-2 focus:text-sm focus:font-medium focus:text-ink">
        {t("skipToContent")}
      </a>
      <div className={cx(LANDING_CONTAINER, "flex h-16 items-center justify-between gap-4")}>
        <Brand alwaysShowName />
        <nav aria-label={t("navMain")} className="hidden items-center gap-1 lg:flex">
          {NAV.map((l) => (
            <Link key={l.href} href={l.href} className="rounded-md px-3 py-2 text-sm text-ink-2 transition hover:text-ink">{t(l.label)}</Link>
          ))}
        </nav>
        <div className="hidden items-center gap-2 lg:flex"><LanguageSwitcher /><AccountActions /></div>
        <button type="button" onClick={() => setOpen((v) => !v)} aria-expanded={open} aria-controls="mobile-menu"
          aria-label={open ? t("closeMenu") : t("openMenu")} className="grid h-11 w-11 place-items-center rounded-md text-ink hover:bg-paper-2 lg:hidden">
          <Icon name={open ? "close" : "menu"} className="h-6 w-6" />
        </button>
      </div>
      {open && (
        <div id="mobile-menu" className="border-t border-rule bg-paper px-4 pb-5 pt-2 lg:hidden">
          <nav aria-label={t("navMobile")} className="flex flex-col">
            {NAV.map((l) => (
              <Link key={l.href} href={l.href} onClick={close} className="border-b border-rule px-1 py-3.5 text-base text-ink">{t(l.label)}</Link>
            ))}
          </nav>
          <div className="mt-4 grid gap-2"><LanguageSwitcher /><AccountActions stacked onNavigate={close} /></div>
        </div>
      )}
    </header>
  );
}

/** Signed-in visitors of /pricing get "Open dashboard"; on "/" they are redirected before this renders. */
function AccountActions({ stacked, onNavigate }: { stacked?: boolean; onNavigate?: () => void }) {
  const { user, loading } = useAuth();
  const t = useT(landingMessages);
  const size = stacked ? "min-h-12 grid place-items-center" : "py-2";
  const secondary = cx("rounded-md px-4 text-sm font-medium text-ink transition hover:bg-paper-2", stacked && "border border-rule-strong", size);
  const primary = cx("rounded-md bg-sidebar px-4 text-sm font-semibold text-white transition hover:bg-sidebar-2", size);
  if (!loading && user) return <Link href="/app" onClick={onNavigate} className={primary}>{t("openDashboard")}</Link>;
  return (
    <>
      <Link href="/login" onClick={onNavigate} className={secondary}>{t("signIn")}</Link>
      <Link href={GET_STARTED_HREF} onClick={onNavigate} className={primary}>{t("getStarted")}</Link>
    </>
  );
}
