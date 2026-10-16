import Link from "next/link";
import { Brand } from "@/components/AppShell";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { LANDING_CONTAINER } from "./landingStyles";

const PRODUCT_LINKS: { href: string; label: LandingKey }[] = [
  { href: "/#workflow", label: "navWorkflow" },
  { href: "/#report", label: "navReport" },
  { href: "/#agencies", label: "navAgencies" },
  { href: "/pricing", label: "navPricing" },
];

const ACCOUNT_LINKS: { href: string; label: LandingKey }[] = [
  { href: "/login", label: "signIn" },
  { href: GET_STARTED_HREF, label: "createAccount" },
];

/** Closing call to action on the navy of the app's sidebar, the one dark band on the page. */
export async function FinalCallToAction() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="cta-title" className="bg-sidebar">
      <div className={cx(LANDING_CONTAINER, "grid gap-8 py-16 sm:py-20 lg:grid-cols-[minmax(0,1.3fr)_auto] lg:items-end")}>
        <div>
          <h2 id="cta-title" className="font-display text-[clamp(2.25rem,4.4vw,3.5rem)] leading-[1.02] tracking-[-0.02em] text-white">{t("ctaTitle")}</h2>
          <p className="mt-4 max-w-[34rem] text-[1.0625rem] leading-relaxed text-sidebar-ink">{t("ctaText")}</p>
        </div>
        <div className="flex flex-col gap-3 sm:flex-row">
          <Link href={GET_STARTED_HREF} className="inline-flex min-h-12 items-center justify-center gap-2 rounded-md bg-white px-5 text-[0.9375rem] font-semibold text-sidebar transition hover:bg-paper">
            {t("getStarted")}<Icon name="arrowRight" className="h-4 w-4" />
          </Link>
          <Link href="/login" className="inline-flex min-h-12 items-center justify-center rounded-md border border-white/25 px-5 text-[0.9375rem] font-medium text-white transition hover:border-white/60">
            {t("signIn")}
          </Link>
        </div>
      </div>
    </section>
  );
}

export async function LandingFooter() {
  const t = await getServerTranslator(landingMessages);
  return (
    <footer className="bg-paper">
      <div className={cx(LANDING_CONTAINER, "grid gap-10 py-12 sm:grid-cols-[minmax(0,1.6fr)_1fr_1fr]")}>
        <div>
          <Brand alwaysShowName />
          <p className="mt-3 max-w-[22rem] text-sm text-paper-muted">{t("footerTagline")}</p>
          <p className="mt-2 text-sm text-paper-muted">{t("footerLanguages")}</p>
        </div>
        <FooterLinks title={t("footerProduct")} links={PRODUCT_LINKS.map((l) => ({ href: l.href, label: t(l.label) }))} label={t("navFooter")} />
        <FooterLinks title={t("footerAccount")} links={ACCOUNT_LINKS.map((l) => ({ href: l.href, label: t(l.label) }))} />
      </div>
      <div className={cx(LANDING_CONTAINER, "border-t border-rule py-5 text-xs text-paper-muted")}>© {new Date().getFullYear()} InspectFlow</div>
    </footer>
  );
}

function FooterLinks({ title, links, label }: { title: string; links: { href: string; label: string }[]; label?: string }) {
  return (
    <nav aria-label={label ?? title}>
      <p className="font-mono text-[0.6875rem] uppercase tracking-[0.16em] text-paper-muted">{title}</p>
      <ul className="mt-4 space-y-2.5 text-sm">
        {links.map((l) => <li key={l.href}><Link href={l.href} className="text-ink-2 transition hover:text-ink">{l.label}</Link></li>)}
      </ul>
    </nav>
  );
}
