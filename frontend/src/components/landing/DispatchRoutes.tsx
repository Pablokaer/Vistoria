import { Icon, type IconName } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { EYEBROW, LANDING_CONTAINER, SECTION_TITLE } from "./landingStyles";

// The three inspection visibilities (Team, Public, Private), with the icons the app uses for them (VisibilityTag).
const ROUTES: { icon: IconName; title: LandingSectionKey; text: LandingSectionKey }[] = [
  { icon: "users", title: "dispatchTeamTitle", text: "dispatchTeamText" },
  { icon: "globe", title: "dispatchMarketTitle", text: "dispatchMarketText" },
  { icon: "lock", title: "dispatchPrivateTitle", text: "dispatchPrivateText" },
];

/** "For agencies": the three ways an inspection reaches an inspector. */
export async function DispatchRoutes() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="agencies" aria-labelledby="agencies-title" className="scroll-mt-16 py-20 sm:py-28">
      <div className={LANDING_CONTAINER}>
        <div className="max-w-[40rem]">
          <p className={EYEBROW}>{t("dispatchEyebrow")}</p>
          <h2 id="agencies-title" className={cx(SECTION_TITLE, "mt-4")}>{t("dispatchTitle")}</h2>
          <p className="mt-5 text-[1.0625rem] leading-relaxed text-ink-2">{t("dispatchIntro")}</p>
        </div>
        <ul className="mt-14 grid border-t-2 border-ink md:grid-cols-3">
          {ROUTES.map((r, index) => (
            <li key={r.title} className={cx("border-b border-rule py-8 md:border-b-0 md:pr-8", index > 0 && "md:border-l md:pl-8")}>
              <span className="grid h-10 w-10 place-items-center rounded-md border border-rule-strong bg-white text-ink">
                <Icon name={r.icon} className="h-5 w-5" />
              </span>
              <h3 className="mt-6 font-display text-[1.625rem] leading-tight text-ink">{t(r.title)}</h3>
              <p className="mt-3 text-[0.9375rem] leading-relaxed text-ink-2">{t(r.text)}</p>
            </li>
          ))}
        </ul>
        <p className="mt-8 flex items-start gap-2 text-[0.9375rem] text-ink-2 md:mt-10">
          <Icon name="arrowRight" className="mt-0.5 h-4 w-4 shrink-0 text-paper-muted" />{t("dispatchAfter")}
        </p>
      </div>
    </section>
  );
}
