import { Icon, type IconName } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { enumMessages } from "@/i18n/messages/enums";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { EYEBROW, LANDING_CONTAINER, SECTION_TITLE } from "./landingStyles";
import { RoomSketch } from "./RoomSketch";

const GUARANTEES: { icon: IconName; title: LandingSectionKey; text: LandingSectionKey }[] = [
  { icon: "lock", title: "guarantee1Title", text: "guarantee1Text" },
  { icon: "compare", title: "guarantee2Title", text: "guarantee2Text" },
  { icon: "sparkles", title: "guarantee3Title", text: "guarantee3Text" },
  { icon: "share", title: "guarantee4Title", text: "guarantee4Text" },
];

// Three of the real comparison decisions (ComparisonDecision); the figure shows "NormalWear" as the one chosen.
const DECISIONS = ["Unchanged", "NormalWear", "NewDamage"] as const;
const CHOSEN_DECISION = "NormalWear";

/** "The report": why the record holds up — immutability, Move In comparison, human decisions, sharing. */
export async function ReportGuarantees() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="report" aria-labelledby="report-title" className="scroll-mt-16 border-y border-rule bg-white py-20 sm:py-28">
      <div className={cx(LANDING_CONTAINER, "grid gap-14 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:gap-20")}>
        <div className="lg:sticky lg:top-28 lg:self-start">
          <p className={EYEBROW}>{t("reportEyebrow")}</p>
          <h2 id="report-title" className={cx(SECTION_TITLE, "mt-4")}>{t("reportTitle")}</h2>
          <p className="mt-5 max-w-[30rem] text-[1.0625rem] leading-relaxed text-ink-2">{t("reportIntro")}</p>
          <CompareFigure />
        </div>
        <dl>
          {GUARANTEES.map((g) => (
            <div key={g.title} className="border-t border-rule py-8 first:border-t-2 first:border-ink sm:pl-12">
              <dt className="relative font-display text-[1.625rem] leading-tight text-ink">
                <Icon name={g.icon} className="mb-3 block h-5 w-5 text-paper-muted sm:absolute sm:-left-12 sm:top-1.5 sm:mb-0" />{t(g.title)}
              </dt>
              <dd className="mt-2 max-w-[34rem] text-[0.9375rem] leading-relaxed text-ink-2">{t(g.text)}</dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}

/** The same room at Move In and Move Out with the inspector's decision, as the agent's comparison screen shows it. */
async function CompareFigure() {
  const t = await getServerTranslator(landingSectionMessages);
  const e = await getServerTranslator(enumMessages);
  return (
    <figure className="mt-10 max-w-[30rem] rounded-md border border-rule bg-paper p-4">
      <div aria-hidden="true" className="grid grid-cols-2 gap-3">
        <div><RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-sm" />
          <p className="mt-1.5 border-l-2 border-movein pl-2 text-xs font-medium text-ink-2">{t("compareMoveIn")}</p></div>
        <div><RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-sm" />
          <p className="mt-1.5 border-l-2 border-moveout pl-2 text-xs font-medium text-ink-2">{t("compareMoveOut")}</p></div>
      </div>
      <div aria-hidden="true" className="mt-4 flex flex-wrap items-center gap-2 border-t border-rule pt-3">
        <span className="mr-1 text-[0.6875rem] uppercase tracking-[0.1em] text-paper-muted">{t("compareDecision")}</span>
        {DECISIONS.map((d) => (
          <span key={d} className={cx("rounded-sm border px-2 py-0.5 text-xs",
            d === CHOSEN_DECISION ? "border-ink bg-ink font-semibold text-white" : "border-rule-strong text-paper-muted")}>{e(d)}</span>
        ))}
      </div>
      <figcaption className="mt-3 text-xs leading-relaxed text-paper-muted">{t("compareCaption")}</figcaption>
    </figure>
  );
}
