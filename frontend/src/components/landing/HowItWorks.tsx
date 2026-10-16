import { getServerTranslator } from "@/i18n/server";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { LANDING_CONTAINER, SECTION_SPACING } from "./landingStyles";
import { Reveal } from "./Reveal";
import { SectionHeading } from "./SectionHeading";

// The real lifecycle in four hand-offs: Open (published) → Assigned → InProgress/Review → Completed (report).
const STEPS: { title: LandingSectionKey; text: LandingSectionKey }[] = [
  { title: "howStep1Title", text: "howStep1Text" },
  { title: "howStep2Title", text: "howStep2Text" },
  { title: "howStep3Title", text: "howStep3Text" },
  { title: "howStep4Title", text: "howStep4Text" },
];

/** "How it works": four numbered stages joined by one line — across on desktop, down the left edge on phones. */
export async function HowItWorks() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="how-it-works" aria-labelledby="how-title" className={cx("bg-site-mist", SECTION_SPACING)}>
      <div className={LANDING_CONTAINER}>
        <SectionHeading id="how-title" eyebrow={t("howEyebrow")} title={t("howTitle")} centered />
        <Reveal>
          <ol className="mx-auto mt-14 grid max-w-[30rem] gap-10 lg:mt-20 lg:max-w-none lg:grid-cols-4 lg:gap-8">
            {STEPS.map((step, index) => (
              <Stage key={step.title} number={index + 1} last={index === STEPS.length - 1} title={t(step.title)} text={t(step.text)} />
            ))}
          </ol>
        </Reveal>
      </div>
    </section>
  );
}

/** The connector runs from this stage's marker to the next one: downwards on phones, rightwards from 1024px. */
function Stage({ number, last, title, text }: { number: number; last: boolean; title: string; text: string }) {
  const connector = !last && cx(
    "before:absolute before:left-5 before:top-12 before:-bottom-8 before:w-px before:bg-site-line-strong",
    "lg:before:left-14 lg:before:right-[-1.5rem] lg:before:top-5 lg:before:bottom-auto lg:before:h-px lg:before:w-auto",
  );
  return (
    <li className={cx("relative pl-16 lg:pl-0", connector)}>
      <span className="tabular absolute left-0 top-0 grid h-10 w-10 place-items-center rounded-full border border-site-line-strong bg-white text-[0.8125rem] font-semibold text-site-blue lg:static">
        {String(number).padStart(2, "0")}
      </span>
      <h3 className="pt-2 text-[1.0625rem] font-semibold tracking-[-0.01em] text-site-ink lg:mt-6 lg:pt-0">{title}</h3>
      <p className="mt-2 text-[0.9375rem] leading-relaxed text-site-muted">{text}</p>
    </li>
  );
}
