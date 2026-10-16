import Image, { type StaticImageData } from "next/image";
import { Icon } from "@/components/icons";
import { getServerTranslator } from "@/i18n/server";
import { captureMessages } from "@/i18n/messages/capture";
import { enumMessages } from "@/i18n/messages/enums";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { landingSectionMessages } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { bedroomBed, bedroomMain, bedroomPlant, bedroomWindow } from "./landingPhotos";

const THUMBS: StaticImageData[] = [bedroomPlant, bedroomWindow, bedroomBed];

/**
 * The inspector's "Descriptions" step on a phone, drawn with the app's own labels (step names, "Take photo", the
 * "AI draft · review and edit" chip and its violet tint) so it shows what the product really does. Decorative for
 * assistive technology; the caption describes it. Example: `<InspectionPhone />`
 */
export async function InspectionPhone() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <figure className="mx-auto w-[17.5rem] sm:w-[18.5rem]">
      <figcaption className="sr-only">{t("phoneCaption")}</figcaption>
      <div aria-hidden="true" className="rounded-[2.5rem] bg-[#0d1626] p-2.5 shadow-[0_40px_80px_-30px_rgb(11_23_40/0.65)] ring-1 ring-white/10">
        <div className="overflow-hidden rounded-[2rem] bg-canvas">
          <PhoneTopBar />
          <PhoneSteps />
          <div className="space-y-3 p-3"><PhoneRoomCard /></div>
        </div>
      </div>
    </figure>
  );
}

async function PhoneTopBar() {
  const t = await getServerTranslator(landingSectionMessages);
  const e = await getServerTranslator(enumMessages);
  return (
    <div className="bg-white px-4 pb-3 pt-3">
      <div className="mx-auto mb-3 h-1.5 w-16 rounded-full bg-ink/10" />
      <div className="flex items-center gap-2">
        <Icon name="chevronLeft" className="h-4 w-4 text-ink-3" />
        <span className="truncate text-[0.8125rem] font-semibold text-ink">{t("phoneAddress")}</span>
        <span className="ml-auto rounded-sm bg-movein-50 px-1.5 py-0.5 text-[0.625rem] font-semibold text-movein">{e("MoveIn")}</span>
      </div>
    </div>
  );
}

/** Photos (done) → Descriptions (current) → Review, as the app's step header shows them. */
async function PhoneSteps() {
  const c = await getServerTranslator(captureMessages);
  const steps = [{ label: c("stepPhotos"), state: "done" }, { label: c("stepDescriptions"), state: "current" }, { label: c("stepReview"), state: "next" }];
  return (
    <ol className="grid grid-cols-3 gap-1 border-y border-line bg-white px-3 pb-2.5 pt-2">
      {steps.map((s) => (
        <li key={s.label} className="min-w-0">
          <span className={cx("block h-1 rounded-full", s.state === "next" ? "bg-line" : "bg-brand")} />
          <span className={cx("mt-1.5 block truncate text-[0.625rem] font-medium", s.state === "current" ? "text-brand" : "text-ink-3")}>{s.label}</span>
        </li>
      ))}
    </ol>
  );
}

async function PhoneRoomCard() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <div className="rounded-lg border border-line bg-white p-3 shadow-card">
      <div className="flex items-baseline justify-between">
        <span className="text-[0.875rem] font-semibold text-ink">{t("phoneRoom")}</span>
        <span className="text-[0.625rem] text-ink-3">{t("phoneRoomCount")}</span>
      </div>
      <Image src={bedroomMain} alt="" sizes="280px" className="mt-2.5 aspect-[4/3] w-full rounded-md object-cover" />
      <PhoneThumbs />
      <PhoneDescription />
    </div>
  );
}

async function PhoneThumbs() {
  const tools = await getServerTranslator(inspectionToolsMessages);
  return (
    <div className="mt-2 grid grid-cols-4 gap-1.5">
      {THUMBS.map((photo) => <Image key={photo.src} src={photo} alt="" sizes="64px" className="aspect-square w-full rounded-[0.3rem] object-cover" />)}
      <span className="flex aspect-square flex-col items-center justify-center gap-0.5 rounded-[0.3rem] border border-dashed border-brand/50 bg-brand-50 text-brand">
        <Icon name="camera" className="h-4 w-4" /><span className="text-center text-[0.5rem] font-semibold leading-tight">{tools("takePhoto")}</span>
      </span>
    </div>
  );
}

async function PhoneDescription() {
  const t = await getServerTranslator(landingSectionMessages);
  const tools = await getServerTranslator(inspectionToolsMessages);
  return (
    <div className="mt-3">
      <div className="flex flex-wrap items-center justify-between gap-1">
        <span className="text-[0.6875rem] font-medium text-ink-2">{t("phoneDescriptionLabel")}</span>
        <span className="inline-flex items-center gap-1 rounded-sm bg-review-50 px-1.5 py-0.5 text-[0.5625rem] font-semibold text-review-700 ring-1 ring-inset ring-review-700/15">
          <Icon name="sparkles" className="h-3 w-3" />{tools("aiStateDraft")}
        </span>
      </div>
      <p className="mt-1.5 rounded-md border border-review-500/40 bg-review-50/40 p-2 text-[0.6875rem] leading-[1.45] text-ink-2">{t("phoneAiText")}</p>
    </div>
  );
}
