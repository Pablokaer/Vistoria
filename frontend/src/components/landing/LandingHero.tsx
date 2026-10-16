import Image from "next/image";
import Link from "next/link";
import { Icon, type IconName } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { GET_STARTED_HREF } from "@/lib/billing";
import { cx } from "@/lib/cx";
import { heroLivingRoom } from "./landingPhotos";
import { LANDING_CONTAINER, PRIMARY_LINK, SECONDARY_LINK } from "./landingStyles";
import { RoomStatusCard } from "./RoomStatusCard";

const TRUST: { icon: IconName; label: LandingKey }[] = [
  { icon: "camera", label: "trustPhotos" },
  { icon: "sparkles", label: "trustAi" },
  { icon: "shield", label: "trustSecure" },
];

/**
 * First impression: headline, the two entry points and what every inspection includes on the left; a bright
 * interior on the right that fades into the text column, with a real room status card over it. Below 1024px the
 * photo follows the text as a framed image, so the actions stay above the fold.
 */
export async function LandingHero() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="hero-title" className="relative overflow-hidden bg-white">
      <div className={cx(LANDING_CONTAINER, "relative z-10 lg:flex lg:min-h-[min(46rem,calc(100svh-4.5rem))] lg:items-center")}>
        <div className="pb-12 pt-10 sm:pb-14 sm:pt-16 lg:w-[48%] lg:py-24">
          <HeroCopy />
          <TrustIndicators label={t("heroTrustLabel")} items={TRUST.map((item) => ({ icon: item.icon, text: t(item.label) }))} />
        </div>
      </div>
      <HeroPhoto alt={t("heroPhotoAlt")} />
    </section>
  );
}

async function HeroCopy() {
  const t = await getServerTranslator(landingMessages);
  return (
    <>
      <h1 id="hero-title" className="text-[clamp(2.375rem,6vw,3.375rem)] font-semibold leading-[1.06] tracking-[-0.035em] text-site-ink">
        {/* Deliberate breaks: the promise always lands on its own line, never split ("made / easy."). The {" "}
            keep the words apart in the text itself (search snippets, copy-paste), which the block spans do not. */}
        <span className="block">{t("heroTitleLine1")}</span>{" "}
        <span className="block">{t("heroTitleLine2")}</span>{" "}
        {/* Below 360px the unbreakable "sem complicação." is wider than the column at 38px; step the size down. */}
        <span className="block whitespace-nowrap text-site-blue max-[359px]:text-[2rem]">{t("heroTitleAccent")}</span>
      </h1>
      <p className="mt-6 max-w-[33rem] text-[1.0625rem] leading-relaxed text-site-muted sm:text-[1.125rem]">{t("heroIntro")}</p>
      <div className="mt-9 flex flex-col gap-3 sm:flex-row">
        <Link href={GET_STARTED_HREF} className={PRIMARY_LINK}>{t("getStarted")}<Icon name="arrowRight" className="h-4 w-4" /></Link>
        <Link href="#how-it-works" className={SECONDARY_LINK}>{t("heroSeeHow")}</Link>
      </div>
    </>
  );
}

function TrustIndicators({ label, items }: { label: string; items: { icon: IconName; text: string }[] }) {
  return (
    // One row where the column is wide enough (tablet), otherwise an even stack rather than a ragged wrap.
    <ul aria-label={label} className="mt-10 grid gap-3 border-t border-site-line pt-6 sm:flex sm:flex-wrap sm:gap-x-6 lg:grid lg:gap-3">
      {items.map((item) => (
        <li key={item.text} className="flex items-center gap-2.5 text-[0.875rem] font-medium text-site-ink">
          <span className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-site-blue-soft text-site-blue"><Icon name={item.icon} className="h-4 w-4" /></span>
          {item.text}
        </li>
      ))}
    </ul>
  );
}

/** Desktop: right 54% of the hero, masked so its left edge dissolves into white. Smaller screens: a framed photo. */
function HeroPhoto({ alt }: { alt: string }) {
  return (
    <div className="relative mx-4 mb-14 sm:mx-6 lg:absolute lg:inset-y-0 lg:right-0 lg:m-0 lg:w-[54%]">
      <div className="relative aspect-[4/3.4] overflow-hidden rounded-xl sm:aspect-[16/10] lg:h-full lg:rounded-none lg:[mask-image:linear-gradient(to_right,transparent,black_24%)]">
        <Image src={heroLivingRoom} alt={alt} fill preload placeholder="blur" sizes="(min-width: 1024px) 54vw, 100vw"
          className="object-cover object-[50%_60%]" />
      </div>
      <RoomStatusCard className="absolute -bottom-8 left-3 right-3 mx-auto sm:left-6 sm:right-auto lg:bottom-[11%] lg:left-[4%]" />
    </div>
  );
}
