import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { Icon, type IconName } from "@/components/icons";
import { SectionHeading } from "./SectionHeading";

const STEPS: { icon: IconName; who: LandingKey; title: LandingKey; text: LandingKey }[] = [
  { icon: "building", who: "whoCompany", title: "step1Title", text: "step1Text" },
  { icon: "clipboard", who: "whoInspector", title: "step2Title", text: "step2Text" },
  { icon: "camera", who: "whoInspector", title: "step3Title", text: "step3Text" },
  { icon: "sparkles", who: "whoAi", title: "step4Title", text: "step4Text" },
  { icon: "check", who: "whoInspector", title: "step5Title", text: "step5Text" },
  { icon: "file", who: "whoPlatform", title: "step6Title", text: "step6Text" },
  { icon: "userCheck", who: "whoTenant", title: "step7Title", text: "step7Text" },
];

export async function HowItWorksSection() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section id="how-it-works" aria-labelledby="how-title" className="scroll-mt-20 bg-white py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="how-title" eyebrow={t("howEyebrow")} title={t("howTitle")} centered />
        <ol className="mt-14 grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((s, i) => (
            <li key={s.title} className="relative">
              <div className="flex items-center gap-3">
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-brand text-sm font-semibold text-white">{i + 1}</span>
                <span className="inline-flex items-center gap-1.5 rounded-full bg-neutral-50 px-2.5 py-1 text-xs font-medium text-ink-3">
                  <Icon name={s.icon} className="h-3.5 w-3.5" />{t(s.who)}
                </span>
              </div>
              <h3 className="mt-4 text-base font-semibold text-ink">{t(s.title)}</h3>
              <p className="mt-1.5 text-sm leading-relaxed text-ink-3">{t(s.text)}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
