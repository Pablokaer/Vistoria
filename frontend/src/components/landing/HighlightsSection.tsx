import { getServerTranslator } from "@/i18n/server";
import { landingDetailsMessages, type LandingDetailsKey } from "@/i18n/messages/landingDetails";
import type { Translate } from "@/i18n/translate";
import { Icon } from "@/components/icons";
import { RoomSketch } from "./RoomSketch";
import { SectionHeading } from "./SectionHeading";

type T = Translate<LandingDetailsKey>;

const AI_POINTS: LandingDetailsKey[] = ["aiPoint1", "aiPoint2", "aiPoint3"];

const REPORT_POINTS: LandingDetailsKey[] = ["reportPoint1", "reportPoint2", "reportPoint3"];

function Points({ items, t }: { items: LandingDetailsKey[]; t: T }) {
  return (
    <ul className="mt-6 space-y-3">
      {items.map((p) => (
        <li key={p} className="flex gap-3 text-ink-2"><Icon name="check" className="mt-1 h-4 w-4 shrink-0 text-brand" strokeWidth={2.5} />{t(p)}</li>
      ))}
    </ul>
  );
}

export async function HighlightsSection() {
  const t = await getServerTranslator(landingDetailsMessages);
  return (
    <section aria-label={t("highlightsLabel")} className="bg-white py-20 sm:py-24">
      <div className="mx-auto grid max-w-6xl gap-20 px-4 sm:px-6">
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <div>
            <SectionHeading eyebrow={t("aiEyebrow")} title={t("aiTitle")} />
            <Points items={AI_POINTS} t={t} />
          </div>
          <AiDraftVisual t={t} />
        </div>
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <ComparisonVisual t={t} />
          <div className="lg:order-first">
            <SectionHeading eyebrow={t("compareEyebrow")} title={t("compareTitle")} />
            <Points items={REPORT_POINTS} t={t} />
          </div>
        </div>
      </div>
    </section>
  );
}

function AiDraftVisual({ t }: { t: T }) {
  return (
    <div className="rounded-xl border border-line bg-surface-2 p-5 sm:p-6">
      <div className="grid grid-cols-2 gap-3">
        <RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-lg" />
        <RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-lg" />
      </div>
      <div className="mt-4 rounded-xl border border-line bg-white p-4">
        <p className="flex items-center gap-1.5 text-xs font-medium text-brand"><Icon name="sparkles" className="h-4 w-4" /> {t("aiDraftLabel")}</p>
        <p className="mt-2 text-sm leading-relaxed text-ink-3">{t("aiDraftSample")}</p>
        <p className="mt-3 border-t border-line pt-3 text-xs text-ink-3">{t("aiDraftApproved")}</p>
      </div>
    </div>
  );
}

function ComparisonVisual({ t }: { t: T }) {
  return (
    <div className="rounded-xl border border-line bg-surface-2 p-5 sm:p-6">
      <p className="text-sm font-semibold text-ink">{t("sampleLivingRoom")}</p>
      <div className="mt-3 grid grid-cols-2 gap-3">
        <figure><RoomSketch variant="living" className="aspect-[4/3] w-full rounded-lg" /><figcaption className="mt-2 text-xs text-ink-3">{t("sampleMoveInDate")}</figcaption></figure>
        <figure><RoomSketch variant="living" scuff className="aspect-[4/3] w-full rounded-lg" /><figcaption className="mt-2 text-xs text-ink-3">{t("sampleMoveOutDate")}</figcaption></figure>
      </div>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-line bg-white px-4 py-3">
        <span className="text-sm text-ink-2">{t("sampleDefect")}</span>
        <span className="rounded-full bg-neutral-50 px-2.5 py-0.5 text-xs font-medium text-ink-2">{t("samplePreExisting")}</span>
      </div>
    </div>
  );
}
