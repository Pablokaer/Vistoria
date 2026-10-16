import { Icon, type IconName } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { LANDING_CONTAINER, SECTION_SPACING } from "./landingStyles";
import { Reveal } from "./Reveal";
import { SectionHeading } from "./SectionHeading";

interface Feature { icon: IconName; title: LandingSectionKey; text: LandingSectionKey; detail: LandingSectionKey }

// One card per stage of the product; the detail line names the concrete mechanism behind each claim.
const FEATURES: Feature[] = [
  { icon: "building", title: "featureRequestsTitle", text: "featureRequestsText", detail: "featureRequestsDetail" },
  { icon: "users", title: "featureAgentsTitle", text: "featureAgentsText", detail: "featureAgentsDetail" },
  { icon: "camera", title: "featureInspectionsTitle", text: "featureInspectionsText", detail: "featureInspectionsDetail" },
  { icon: "file", title: "featureReportsTitle", text: "featureReportsText", detail: "featureReportsDetail" },
];

/** "Features": the four things the platform does, from request to report. */
export async function FeatureGrid() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="features" aria-labelledby="features-title" className={cx("bg-site-mist", SECTION_SPACING)}>
      <div className={LANDING_CONTAINER}>
        <SectionHeading id="features-title" eyebrow={t("featuresEyebrow")} title={t("featuresTitle")} intro={t("featuresIntro")} />
        <Reveal>
          {/* Four columns only from 1280px: at 1024px each card would be ~230px wide and the text would run long. */}
          <ul className="mt-14 grid gap-4 sm:grid-cols-2 lg:mt-16 lg:gap-5 xl:grid-cols-4">
            {FEATURES.map((f) => <FeatureCard key={f.title} icon={f.icon} title={t(f.title)} text={t(f.text)} detail={t(f.detail)} />)}
          </ul>
        </Reveal>
      </div>
    </section>
  );
}

function FeatureCard({ icon, title, text, detail }: { icon: IconName; title: string; text: string; detail: string }) {
  return (
    <li className="flex flex-col rounded-xl border border-site-line bg-white p-6 transition duration-300 hover:-translate-y-0.5 hover:border-site-line-strong hover:shadow-site-card">
      <span className="grid h-10 w-10 place-items-center rounded-lg bg-site-blue-soft text-site-blue"><Icon name={icon} className="h-5 w-5" /></span>
      <div className="flex-1">
        <h3 className="mt-6 text-[1.0625rem] font-semibold tracking-[-0.01em] text-site-ink">{title}</h3>
        <p className="mt-2 text-[0.9375rem] leading-relaxed text-site-muted">{text}</p>
      </div>
      <p className="mt-6 flex items-start gap-2 border-t border-site-line pt-4 text-[0.8125rem] font-medium text-site-ink">
        <Icon name="check" className="mt-px h-4 w-4 shrink-0 text-site-blue" strokeWidth={2.25} />{detail}
      </p>
    </li>
  );
}
