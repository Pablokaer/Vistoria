import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { RoomSketch } from "./RoomSketch";

// A representative (not real) report number in the format FinalizationService.NewReportNumber produces.
const SAMPLE_REPORT_NUMBER = "IR-202609-7KQ4M2XZ";
const SAMPLE_HASH = "9c1f 4a07 … 2be8 e47a";

/**
 * Hero illustration: one room of a finalized Move Out report, drawn from the real report's parts (Move In / Move Out
 * photos, labelled AI draft, defect with the inspector's decision, version and SHA-256 footer). Decorative for
 * assistive technology; the caption describes it. Example: `<ReportSpecimen />`
 */
export async function ReportSpecimen() {
  const t = await getServerTranslator(landingMessages);
  return (
    <figure aria-labelledby="specimen-caption" className="relative mx-auto w-full max-w-[34rem] animate-rise lg:mx-0 lg:justify-self-end">
      <figcaption id="specimen-caption" className="sr-only">{t("specimenCaption")}</figcaption>
      <div aria-hidden="true" className="absolute inset-0 translate-x-3 translate-y-3 rounded-md border border-rule bg-paper-2" />
      <div aria-hidden="true" className="relative rounded-md border border-rule-strong bg-white shadow-[0_1px_0_rgb(15_27_51/0.04),0_28px_56px_-28px_rgb(15_27_51/0.35)]">
        <div className="flex items-center justify-between gap-3 border-b border-rule px-5 py-3">
          <span className="text-[0.8125rem] font-semibold text-ink">{t("specimenKind")}</span>
          <span className="font-mono text-[0.6875rem] tracking-wide text-paper-muted">{SAMPLE_REPORT_NUMBER}</span>
        </div>
        <div className="px-5 pb-5 pt-4">
          <p className="font-display text-[1.375rem] leading-tight text-ink">{t("specimenAddress")}</p>
          <p className="mt-1 text-[0.8125rem] text-paper-muted">{t("specimenRoom")} · {t("specimenRoomCount")}</p>
          <SpecimenPhotos moveIn={t("specimenMoveIn")} moveOut={t("specimenMoveOut")} />
          <div className="mt-4 border-l-2 border-ink-4 pl-3">
            <p className="flex items-center gap-1.5 text-[0.6875rem] font-medium uppercase tracking-[0.08em] text-paper-muted">
              <Icon name="sparkles" className="h-3.5 w-3.5" />{t("specimenAiLabel")}
            </p>
            <p className="mt-1 text-[0.8125rem] leading-relaxed text-ink-2">{t("specimenAiText")}</p>
          </div>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-3 border-t border-rule pt-4">
            <p className="flex items-center gap-2 text-[0.8125rem] text-ink"><Icon name="alert" className="h-4 w-4 text-moveout" />{t("specimenDefect")}</p>
            <div className="sm:text-right">
              <p className="text-[0.6875rem] uppercase tracking-[0.08em] text-paper-muted">{t("specimenDecisionLabel")}</p>
              <p className="mt-1 inline-block rounded-sm bg-moveout-50 px-2 py-0.5 text-[0.8125rem] font-semibold text-moveout">{t("specimenDecision")}</p>
            </div>
          </div>
        </div>
        <SpecimenFooter finalized={t("specimenFinalized")} tenant={t("specimenTenant")} />
      </div>
    </figure>
  );
}

function SpecimenPhotos({ moveIn, moveOut }: { moveIn: string; moveOut: string }) {
  return (
    <div className="mt-4 grid grid-cols-2 gap-3">
      <div>
        <RoomSketch variant="living" className="aspect-[4/3] w-full rounded-sm" />
        <p className="mt-1.5 border-l-2 border-movein pl-2 text-[0.75rem] font-medium text-ink-2">{moveIn}</p>
      </div>
      <div>
        <RoomSketch variant="living" scuff className="aspect-[4/3] w-full rounded-sm" />
        <p className="mt-1.5 border-l-2 border-moveout pl-2 text-[0.75rem] font-medium text-ink-2">{moveOut}</p>
      </div>
    </div>
  );
}

function SpecimenFooter({ finalized, tenant }: { finalized: string; tenant: string }) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 rounded-b-md border-t border-rule bg-paper px-5 py-2.5 text-[0.6875rem] text-paper-muted">
      <span className="font-mono">SHA-256 · {SAMPLE_HASH}</span>
      <span className="flex items-center gap-3">
        <span className="flex items-center gap-1"><Icon name="lock" className="h-3.5 w-3.5" />{finalized}</span>
        <span className="hidden items-center gap-1 sm:flex"><Icon name="clock" className="h-3.5 w-3.5" />{tenant}</span>
      </span>
    </div>
  );
}
