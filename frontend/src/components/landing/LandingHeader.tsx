"use client";

import Link from "next/link";
import { useState } from "react";
import { Brand } from "@/components/AppShell";
import { useAuth } from "@/lib/auth";
import { GET_STARTED_HREF } from "@/lib/billing";
import { useT } from "@/i18n/I18nProvider";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { Icon } from "@/components/icons";

const NAV: { href: string; label: LandingKey }[] = [
  { href: "/#how-it-works", label: "navHowItWorks" },
  { href: "/#features", label: "navFeatures" },
  { href: "/#pricing", label: "navPricing" },
];

/** Public header: section links, Sign in and the primary "Get started" CTA; collapses into a menu on phones. */
export function LandingHeader() {
  const [open, setOpen] = useState(false);
  const t = useT(landingMessages);
  const close = () => setOpen(false);
  return (
    <header className="sticky top-0 z-30 border-b border-line bg-white/95 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Brand alwaysShowName />
        <nav aria-label={t("navMain")} className="hidden items-center gap-1 md:flex">
          {NAV.map((l) => (
            <Link key={l.href} href={l.href} className="rounded-md px-3 py-2 text-sm font-medium text-ink-3 hover:text-ink">{t(l.label)}</Link>
          ))}
        </nav>
        <div className="hidden items-center gap-2 md:flex"><LanguageSwitcher /><AccountActions /></div>
        <button type="button" onClick={() => setOpen((v) => !v)} aria-expanded={open} aria-controls="mobile-menu"
          aria-label={open ? t("closeMenu") : t("openMenu")} className="rounded-md p-2 text-ink-2 hover:bg-neutral-50 md:hidden">
          <Icon name={open ? "close" : "menu"} className="h-6 w-6" />
        </button>
      </div>
      {open && (
        <div id="mobile-menu" className="border-t border-line bg-white px-4 pb-5 pt-2 md:hidden">
          <nav aria-label={t("navMobile")} className="flex flex-col">
            {NAV.map((l) => (
              <Link key={l.href} href={l.href} onClick={close} className="rounded-md px-2 py-3 text-base font-medium text-ink-2 hover:bg-surface-2">{t(l.label)}</Link>
            ))}
          </nav>
          <div className="mt-3 grid gap-2"><LanguageSwitcher /><AccountActions stacked onNavigate={close} /></div>
        </div>
      )}
    </header>
  );
}

function AccountActions({ stacked, onNavigate }: { stacked?: boolean; onNavigate?: () => void }) {
  const { user, loading } = useAuth();
  const t = useT(landingMessages);
  const secondary = `rounded-lg px-4 text-sm font-medium text-ink-2 hover:bg-neutral-50 ${stacked ? "min-h-12 border border-line-strong grid place-items-center" : "py-2"}`;
  const primary = `rounded-lg bg-brand px-4 text-sm font-semibold text-white shadow-card hover:bg-brand-dark ${stacked ? "min-h-12 grid place-items-center" : "py-2"}`;
  if (!loading && user) return <Link href="/app" onClick={onNavigate} className={primary}>{t("openDashboard")}</Link>;
  return (
    <>
      <Link href="/login" onClick={onNavigate} className={secondary}>{t("signIn")}</Link>
      <Link href={GET_STARTED_HREF} onClick={onNavigate} className={primary}>{t("getStarted")}</Link>
    </>
  );
}
