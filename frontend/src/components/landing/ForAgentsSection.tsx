import Image from "next/image";
import Link from "next/link";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { JOIN_AS_AGENT_HREF } from "./landingLinks";
import { agentMoveInVisit } from "./landingPhotos";
import { LANDING_CONTAINER, PRIMARY_LINK, SECTION_SPACING } from "./landingStyles";
import { Reveal } from "./Reveal";
import { SectionHeading } from "./SectionHeading";

const AGENT_BENEFITS: LandingSectionKey[] = ["agentsBenefit1", "agentsBenefit2", "agentsBenefit3"];

/** "For agents": why an inspector joins, and the free sign-up. */
export async function ForAgentsSection() {
  const [t, common] = await Promise.all([getServerTranslator(landingSectionMessages), getServerTranslator(landingMessages)]);
  return (
    <section id="agents" aria-labelledby="agents-title" className={cx("bg-white", SECTION_SPACING)}>
      <div className={cx(LANDING_CONTAINER, "grid items-center gap-14 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)] lg:gap-20")}>
        <div>
          <SectionHeading id="agents-title" eyebrow={t("agentsEyebrow")} title={t("agentsTitle")} intro={t("agentsText")} />
          <AgentBenefits items={AGENT_BENEFITS.map((key) => t(key))} />
          <div className="mt-10 flex flex-col gap-3 sm:flex-row sm:items-center sm:gap-5">
            <Link href={JOIN_AS_AGENT_HREF} className={PRIMARY_LINK}>{common("joinAsAgent")}<Icon name="arrowRight" className="h-4 w-4" /></Link>
            <span className="text-[0.875rem] text-site-muted">{t("agentsFree")}</span>
          </div>
        </div>
        <Reveal><AgentPhoto alt={t("agentsPhotoAlt")} cardTitle={t("agentsCardTitle")} cardMeta={t("agentsCardMeta")} /></Reveal>
      </div>
    </section>
  );
}

function AgentBenefits({ items }: { items: string[] }) {
  return (
    <ul className="mt-8 space-y-3.5">
      {items.map((text) => (
        <li key={text} className="flex gap-3 text-[1rem] text-site-ink">
          <Icon name="checkCircle" className="mt-0.5 h-5 w-5 shrink-0 text-site-blue" />{text}
        </li>
      ))}
    </ul>
  );
}

function AgentPhoto({ alt, cardTitle, cardMeta }: { alt: string; cardTitle: string; cardMeta: string }) {
  return (
    <div className="relative mx-auto max-w-[32rem] lg:mr-0">
      <Image src={agentMoveInVisit} alt={alt} placeholder="blur" sizes="(min-width: 1024px) 32rem, 100vw" className="aspect-[4/5] w-full rounded-2xl object-cover" />
      <div aria-hidden="true" className="absolute -left-3 bottom-8 flex items-center gap-3 rounded-xl border border-site-line bg-white py-3 pl-3 pr-5 shadow-site-float sm:-left-8">
        <span className="grid h-9 w-9 place-items-center rounded-full bg-success-50 text-success-700"><Icon name="check" className="h-5 w-5" strokeWidth={2.25} /></span>
        <span>
          <span className="block text-[0.875rem] font-semibold text-site-ink">{cardTitle}</span>
          <span className="block text-[0.75rem] text-site-muted">{cardMeta}</span>
        </span>
      </div>
    </div>
  );
}
