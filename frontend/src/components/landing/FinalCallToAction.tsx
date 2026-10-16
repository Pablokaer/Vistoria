import Image from "next/image";
import Link from "next/link";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { homeAtDusk } from "./landingPhotos";
import { LANDING_CONTAINER, PRIMARY_LINK } from "./landingStyles";

/**
 * Closing call to action: a home at dusk under a night-blue veil that is solid behind the text and opens up towards
 * the lit windows on the right. The photo is decorative (empty alt); the text never sits on the bright part.
 */
export async function FinalCallToAction() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="cta-title" className="relative isolate overflow-hidden bg-site-night">
      {/* Quality 50: under an 80-95% veil the compression is invisible, and this is the page's heaviest photo. */}
      <Image src={homeAtDusk} alt="" fill sizes="100vw" quality={50} placeholder="blur" className="-z-20 object-cover object-[70%_55%]" />
      <div className="absolute inset-0 -z-10 bg-site-night/80 lg:bg-transparent lg:bg-[linear-gradient(90deg,rgb(11_23_40/0.95)_0%,rgb(11_23_40/0.86)_42%,rgb(11_23_40/0.35)_100%)]" />
      <div className={cx(LANDING_CONTAINER, "py-24 sm:py-28 lg:py-36")}>
        <div className="max-w-[34rem]">
          <h2 id="cta-title" className="text-balance text-[clamp(2rem,4.2vw,3.25rem)] font-semibold leading-[1.08] tracking-[-0.03em] text-white">{t("ctaTitle")}</h2>
          <p className="mt-5 text-[1.0625rem] leading-relaxed text-site-night-ink">{t("ctaText")}</p>
          <div className="mt-10 flex flex-col gap-3 sm:flex-row">
            <Link href={GET_STARTED_HREF} className={PRIMARY_LINK}>{t("ctaGetStarted")}<Icon name="arrowRight" className="h-4 w-4" /></Link>
            <Link href="/login" className="inline-flex min-h-12 items-center justify-center rounded-lg border border-white/30 px-5 text-[0.9375rem] font-semibold text-white transition hover:border-white/70 hover:bg-white/5">
              {t("signIn")}
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
