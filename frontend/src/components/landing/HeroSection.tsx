import Link from "next/link";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { Icon } from "./icons";
import { GET_STARTED_HREF } from "@/lib/billing";
import { ProductPreview } from "./ProductPreview";

const PROOF_POINTS: LandingKey[] = ["heroProof1", "heroProof2", "heroProof3"];

export async function HeroSection() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section className="relative overflow-hidden bg-white">
      <div className="absolute inset-x-0 top-0 -z-0 h-[480px] bg-gradient-to-b from-brand-50/70 to-white" aria-hidden="true" />
      <div className="relative mx-auto grid max-w-6xl items-center gap-14 px-4 pb-20 pt-14 sm:px-6 lg:grid-cols-[1fr_1.05fr] lg:pb-28 lg:pt-20">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full border border-brand/15 bg-white px-3 py-1 text-xs font-medium text-brand">
            <Icon name="clipboard" className="h-3.5 w-3.5" /> {t("heroBadge")}
          </p>
          <h1 className="mt-5 text-4xl font-semibold leading-[1.1] tracking-tight text-ink sm:text-5xl">
            {t("heroTitleStart")}<span className="text-brand">{t("heroTitleHighlight")}</span>{t("heroTitleEnd")}
          </h1>
          <p className="mt-5 max-w-xl text-lg leading-relaxed text-slate-600">{t("heroIntro")}</p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row">
            <Link href={GET_STARTED_HREF} className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-brand px-6 text-base font-semibold text-white shadow-sm hover:bg-brand-dark">
              {t("getStarted")} <Icon name="arrowRight" className="h-4 w-4" />
            </Link>
            <Link href="#how-it-works" className="inline-flex min-h-12 items-center justify-center rounded-lg border border-slate-300 bg-white px-6 text-base font-medium text-slate-800 hover:bg-slate-50">
              {t("heroSeeHow")}
            </Link>
          </div>
          <ul className="mt-8 flex flex-col gap-2 text-sm text-slate-600 sm:flex-row sm:flex-wrap sm:gap-x-6">
            {PROOF_POINTS.map((p) => (
              <li key={p} className="flex items-center gap-2"><Icon name="check" className="h-4 w-4 text-brand" strokeWidth={2.5} />{t(p)}</li>
            ))}
          </ul>
        </div>
        <ProductPreview />
      </div>
    </section>
  );
}
