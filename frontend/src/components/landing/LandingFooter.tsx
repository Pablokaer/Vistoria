import Link from "next/link";
import { Brand } from "@/components/AppShell";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { Icon } from "@/components/icons";
import { GET_STARTED_HREF } from "@/lib/billing";

export async function FinalCallToAction() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="cta-title" className="bg-white pb-20 sm:pb-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <div className="rounded-3xl bg-brand px-6 py-12 text-center sm:px-12 sm:py-16">
          <h2 id="cta-title" className="text-3xl font-semibold tracking-tight text-white sm:text-4xl">{t("ctaTitle")}</h2>
          <p className="mx-auto mt-4 max-w-xl text-lg text-brand-100">{t("ctaText")}</p>
          <Link href={GET_STARTED_HREF} className="mt-8 inline-flex min-h-12 items-center gap-2 rounded-lg bg-white px-6 text-base font-semibold text-brand hover:bg-brand-50">
            {t("getStarted")} <Icon name="arrowRight" className="h-4 w-4" />
          </Link>
        </div>
      </div>
    </section>
  );
}

export async function LandingFooter() {
  const t = await getServerTranslator(landingMessages);
  return (
    <footer className="border-t border-line bg-surface-2">
      <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-10 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <div>
          <Brand alwaysShowName />
          <p className="mt-2 text-sm text-ink-3">{t("footerTagline")}</p>
        </div>
        <nav aria-label={t("navFooter")} className="flex flex-wrap gap-x-6 gap-y-2 text-sm text-ink-3">
          <Link href="/#how-it-works" className="hover:text-ink">{t("navHowItWorks")}</Link>
          <Link href="/#features" className="hover:text-ink">{t("navFeatures")}</Link>
          <Link href="/pricing" className="hover:text-ink">{t("navPricing")}</Link>
          <Link href="/login" className="hover:text-ink">{t("signIn")}</Link>
        </nav>
      </div>
    </footer>
  );
}
