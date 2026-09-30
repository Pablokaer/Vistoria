import { getServerTranslator } from "@/i18n/server";
import { landingMessages, type LandingKey } from "@/i18n/messages/landing";
import { Icon, type IconName } from "@/components/icons";
import { SectionHeading } from "./SectionHeading";

const AUDIENCES: { icon: IconName; title: LandingKey; text: LandingKey; benefits: LandingKey[] }[] = [
  {
    icon: "building", title: "audienceCompanyTitle", text: "audienceCompanyText",
    benefits: ["audienceCompany1", "audienceCompany2", "audienceCompany3"],
  },
  {
    icon: "phone", title: "audienceInspectorTitle", text: "audienceInspectorText",
    benefits: ["audienceInspector1", "audienceInspector2", "audienceInspector3"],
  },
  {
    icon: "userCheck", title: "audienceTenantTitle", text: "audienceTenantText",
    benefits: ["audienceTenant1", "audienceTenant2", "audienceTenant3"],
  },
];

export async function AudienceSection() {
  const t = await getServerTranslator(landingMessages);
  return (
    <section aria-labelledby="audience-title" className="border-y border-line bg-surface-2 py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="audience-title" eyebrow={t("audienceEyebrow")} title={t("audienceTitle")} intro={t("audienceIntro")} />
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {AUDIENCES.map((a) => (
            <article key={a.title} className="rounded-xl border border-line bg-white p-6">
              <span className="grid h-10 w-10 place-items-center rounded-lg bg-brand-50 text-brand"><Icon name={a.icon} className="h-5 w-5" /></span>
              <h3 className="mt-4 text-lg font-semibold text-ink">{t(a.title)}</h3>
              <p className="mt-1 text-sm text-ink-3">{t(a.text)}</p>
              <ul className="mt-4 space-y-2">
                {a.benefits.map((b) => (
                  <li key={b} className="flex gap-2 text-sm text-ink-2"><Icon name="check" className="mt-0.5 h-4 w-4 shrink-0 text-brand" strokeWidth={2.5} />{t(b)}</li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
