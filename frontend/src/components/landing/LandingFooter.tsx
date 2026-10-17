import Link from "next/link";
import { Brand } from "@/components/AppShell";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { GET_STARTED_HREF } from "@/lib/billing";
import { PRODUCT_NAME } from "@/lib/brand";
import { cx } from "@/lib/cx";
import { INSPECTOR_SIGN_IN_HREF } from "./landingLinks";
import { LANDING_CONTAINER } from "./landingStyles";

const PRODUCT_LINKS: { href: string; label: LandingKey }[] = [
  { href: "/#features", label: "navFeatures" },
  { href: "/#how-it-works", label: "navHowItWorks" },
  { href: "/#agents", label: "navAgents" },
  { href: "/pricing", label: "navPricing" },
];

const ACCOUNT_LINKS: { href: string; label: LandingKey }[] = [
  { href: "/login", label: "signIn" },
  { href: GET_STARTED_HREF, label: "createCompanyAccount" },
  { href: INSPECTOR_SIGN_IN_HREF, label: "inspectorSignIn" },
];

/** Logo, product and account links. Only real destinations: there are no legal or contact pages to link yet. */
export async function LandingFooter() {
  const t = await getServerTranslator(landingMessages);
  return (
    <footer className="border-t border-site-line bg-white">
      <div className={cx(LANDING_CONTAINER, "grid gap-10 py-14 sm:grid-cols-[minmax(0,1.6fr)_1fr_1fr]")}>
        <div>
          <Brand alwaysShowName />
          <p className="mt-4 max-w-[22rem] text-[0.875rem] leading-relaxed text-site-muted">{t("footerTagline")}</p>
        </div>
        <FooterLinks title={t("footerProduct")} label={t("navFooter")} links={PRODUCT_LINKS.map((l) => ({ href: l.href, label: t(l.label) }))} />
        <FooterLinks title={t("footerAccount")} links={ACCOUNT_LINKS.map((l) => ({ href: l.href, label: t(l.label) }))} />
      </div>
      <div className={LANDING_CONTAINER}>
        <div className="flex flex-col gap-1 border-t border-site-line py-6 text-[0.75rem] text-site-muted sm:flex-row sm:justify-between">
          <span>© {new Date().getFullYear()} {PRODUCT_NAME}</span>
          <span>{t("footerLanguages")}</span>
        </div>
      </div>
    </footer>
  );
}

function FooterLinks({ title, links, label }: { title: string; links: { href: string; label: string }[]; label?: string }) {
  return (
    <nav aria-label={label ?? title}>
      <p className="text-[0.75rem] font-semibold uppercase tracking-[0.12em] text-site-ink">{title}</p>
      <ul className="mt-4 space-y-1">
        {links.map((l) => (
          <li key={l.href}><Link href={l.href} className="inline-flex min-h-9 items-center text-[0.875rem] text-site-muted transition hover:text-site-ink">{l.label}</Link></li>
        ))}
      </ul>
    </nav>
  );
}
