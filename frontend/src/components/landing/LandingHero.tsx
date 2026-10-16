import Link from "next/link";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { EYEBROW, LANDING_CONTAINER, PRIMARY_LINK, SECONDARY_LINK } from "./landingStyles";
import { ReportSpecimen } from "./ReportSpecimen";

/** Value proposition, the two entry points (register / see how it works) and a specimen of the report. */
export async function LandingHero() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="hero-title" className="border-b border-rule">
      <div className={cx(LANDING_CONTAINER, "grid items-center gap-14 pb-16 pt-12 sm:pt-16 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)] lg:gap-14 lg:pb-24 lg:pt-24")}>
        <div>
          <p className={cx(EYEBROW, "flex items-center gap-2")}>
            <span aria-hidden="true" className="flex gap-1"><span className="h-1.5 w-1.5 rounded-full bg-movein" /><span className="h-1.5 w-1.5 rounded-full bg-moveout" /></span>
            {t("heroEyebrow")}
          </p>
          <h1 id="hero-title" className="mt-6 font-display text-[clamp(2.75rem,5.4vw,4.375rem)] leading-[0.98] tracking-[-0.025em] text-ink">
            <span className="block">{t("heroTitleLine1")}</span>
            <span className="block italic text-ink-2">{t("heroTitleLine2")}</span>
          </h1>
          <p className="mt-7 max-w-[34rem] text-[1.0625rem] leading-relaxed text-ink-2">{t("heroIntro")}</p>
          <div className="mt-9 flex flex-col gap-3 sm:flex-row">
            <Link href={GET_STARTED_HREF} className={PRIMARY_LINK}>{t("getStarted")}<Icon name="arrowRight" className="h-4 w-4" /></Link>
            <Link href="#workflow" className={SECONDARY_LINK}>{t("heroSeeHow")}</Link>
          </div>
          <p className="mt-8 max-w-[34rem] border-t border-rule pt-4 text-sm text-paper-muted">{t("heroNote")}</p>
        </div>
        <ReportSpecimen />
      </div>
    </section>
  );
}
