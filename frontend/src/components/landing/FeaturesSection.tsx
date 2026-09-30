import { getServerTranslator } from "@/i18n/server";
import { landingDetailsMessages, type LandingDetailsKey } from "@/i18n/messages/landingDetails";
import { Icon, type IconName } from "@/components/icons";
import { SectionHeading } from "./SectionHeading";

// Only features that exist in the product today (see README → "How to test the flows").
const FEATURES: { icon: IconName; title: LandingDetailsKey; text: LandingDetailsKey }[] = [
  { icon: "building", title: "featPropertyTitle", text: "featPropertyText" },
  { icon: "doorIn", title: "featMoveInTitle", text: "featMoveInText" },
  { icon: "doorOut", title: "featMoveOutTitle", text: "featMoveOutText" },
  { icon: "rooms", title: "featRoomsTitle", text: "featRoomsText" },
  { icon: "camera", title: "featPhotosTitle", text: "featPhotosText" },
  { icon: "sparkles", title: "featAiTitle", text: "featAiText" },
  { icon: "alert", title: "featDefectsTitle", text: "featDefectsText" },
  { icon: "compare", title: "featCompareTitle", text: "featCompareText" },
  { icon: "file", title: "featPdfTitle", text: "featPdfText" },
  { icon: "userCheck", title: "featTenantTitle", text: "featTenantText" },
  { icon: "history", title: "featHistoryTitle", text: "featHistoryText" },
];

export async function FeaturesSection() {
  const t = await getServerTranslator(landingDetailsMessages);
  return (
    <section id="features" aria-labelledby="features-title" className="scroll-mt-20 border-t border-line bg-surface-2 py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="features-title" eyebrow={t("featuresEyebrow")} title={t("featuresTitle")} intro={t("featuresIntro")} />
        <dl className="mt-12 grid gap-x-8 gap-y-8 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((f) => (
            <div key={f.title} className="flex gap-4">
              <dt className="shrink-0">
                <span className="grid h-10 w-10 place-items-center rounded-lg border border-line bg-white text-brand shadow-card">
                  <Icon name={f.icon} className="h-5 w-5" />
                </span>
              </dt>
              <dd>
                <p className="font-semibold text-ink">{t(f.title)}</p>
                <p className="mt-1 text-sm leading-relaxed text-ink-3">{t(f.text)}</p>
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
