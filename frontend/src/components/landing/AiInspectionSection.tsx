import Image from "next/image";
import { Icon, type IconName } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { InspectionPhone, PHONE_PHOTO_SIZES } from "./InspectionPhone";
import { bedroomMain } from "./landingPhotos";
import { LANDING_CONTAINER, SECTION_SPACING } from "./landingStyles";
import { Reveal } from "./Reveal";
import { SectionHeading } from "./SectionHeading";

const BENEFITS: { icon: IconName; title: LandingSectionKey; text: LandingSectionKey }[] = [
  { icon: "sparkles", title: "aiBenefitAnalysisTitle", text: "aiBenefitAnalysisText" },
  { icon: "layers", title: "aiBenefitConsistentTitle", text: "aiBenefitConsistentText" },
  { icon: "clock", title: "aiBenefitTimeTitle", text: "aiBenefitTimeText" },
];

/**
 * "AI-powered inspections": the phone in front of the very room it is documenting, and the honest promise next to
 * it — AI drafts from photos, the inspector reviews (ADR 0011, 0013).
 */
export async function AiInspectionSection() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="ai" aria-labelledby="ai-title" className={cx("bg-white", SECTION_SPACING)}>
      <div className={cx(LANDING_CONTAINER, "grid items-center gap-14 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,1fr)] lg:gap-20")}>
        <Reveal><PhoneStage /></Reveal>
        <div>
          <SectionHeading id="ai-title" eyebrow={t("aiEyebrow")} title={t("aiTitle")} intro={t("aiIntro")} />
          <ul className="mt-10 space-y-6">
            {BENEFITS.map((b) => <Benefit key={b.title} icon={b.icon} title={t(b.title)} text={t(b.text)} />)}
          </ul>
          <p className="mt-10 flex gap-3 rounded-lg border border-site-line bg-site-mist p-4 text-[0.875rem] leading-relaxed text-site-muted">
            <Icon name="userCheck" className="mt-0.5 h-5 w-5 shrink-0 text-site-blue" />{t("aiReviewNote")}
          </p>
        </div>
      </div>
    </section>
  );
}

/**
 * The bedroom photo as the backdrop, softened, with the phone standing in front of it. The backdrop requests the
 * phone photo's size: blurred, the lower resolution is invisible, and the photo is downloaded once, not twice.
 */
function PhoneStage() {
  return (
    <div className="relative isolate overflow-hidden rounded-2xl px-4 py-10 sm:py-14">
      <Image src={bedroomMain} alt="" fill sizes={PHONE_PHOTO_SIZES} className="-z-10 scale-110 object-cover object-[60%_50%] blur-[3px]" />
      <div className="absolute inset-0 -z-10 bg-[linear-gradient(180deg,rgb(11_23_40/0.08),rgb(11_23_40/0.38))]" />
      <InspectionPhone />
    </div>
  );
}

function Benefit({ icon, title, text }: { icon: IconName; title: string; text: string }) {
  return (
    <li className="flex gap-4">
      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-site-blue-soft text-site-blue"><Icon name={icon} className="h-5 w-5" /></span>
      <div>
        <h3 className="text-[1rem] font-semibold text-site-ink">{title}</h3>
        <p className="mt-1 text-[0.9375rem] leading-relaxed text-site-muted">{text}</p>
      </div>
    </li>
  );
}
