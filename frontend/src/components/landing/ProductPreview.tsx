// Hero composition: a faithful, simplified rendering of the real inspection screens (rooms, photos,
// AI-drafted text, defects), plus the Move In vs Move Out comparison and the finalized report.
import { getServerTranslator } from "@/i18n/server";
import { landingDetailsMessages, type LandingDetailsKey } from "@/i18n/messages/landingDetails";
import type { Translate } from "@/i18n/translate";
import { Icon } from "./icons";
import { RoomSketch } from "./RoomSketch";

type T = Translate<LandingDetailsKey>;

const ROOMS: { name: LandingDetailsKey; done: boolean }[] = [
  { name: "sampleLivingRoom", done: true },
  { name: "previewKitchen", done: true },
  { name: "previewBedroom1", done: true },
  { name: "previewBedroom2", done: true },
  { name: "previewBathroom", done: false },
];

export async function ProductPreview() {
  const t = await getServerTranslator(landingDetailsMessages);
  return (
    <div className="relative mx-auto w-full max-w-xl lg:mb-24 lg:max-w-none" aria-label={t("previewLabel")} role="img">
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-[0_24px_60px_-24px_rgba(15,23,42,0.25)]">
        <BrowserBar t={t} />
        <div className="grid gap-4 p-4 sm:grid-cols-[150px_1fr] sm:p-5">
          <RoomList t={t} />
          <RoomDetail t={t} />
        </div>
      </div>
      <ComparisonCard t={t} />
      <ReportCard t={t} />
    </div>
  );
}

function BrowserBar({ t }: { t: T }) {
  return (
    <div className="flex items-center gap-2 border-b border-slate-100 bg-slate-50 px-4 py-2.5">
      <span className="h-2.5 w-2.5 rounded-full bg-slate-300" /><span className="h-2.5 w-2.5 rounded-full bg-slate-300" /><span className="h-2.5 w-2.5 rounded-full bg-slate-300" />
      <span className="ml-3 truncate rounded-md bg-white px-3 py-1 text-[11px] text-slate-500 ring-1 ring-slate-200">{t("previewAddress")}</span>
    </div>
  );
}

function RoomList({ t }: { t: T }) {
  return (
    <div className="hidden sm:block">
      <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-400">{t("previewRoomsProgress")}</p>
      <div className="mt-2 h-1.5 rounded-full bg-slate-100"><div className="h-full w-4/5 rounded-full bg-brand" /></div>
      <ul className="mt-3 space-y-1.5">
        {ROOMS.map((r) => (
          <li key={r.name} className={`flex items-center gap-2 rounded-md px-2 py-1.5 text-xs ${r.name === "sampleLivingRoom" ? "bg-brand-50 font-medium text-brand" : "text-slate-600"}`}>
            <span className={`grid h-4 w-4 place-items-center rounded-full ${r.done ? "bg-emerald-500 text-white" : "border border-slate-300"}`}>
              {r.done && <Icon name="check" className="h-3 w-3" strokeWidth={3} />}
            </span>
            {t(r.name)}
          </li>
        ))}
      </ul>
    </div>
  );
}

function RoomDetail({ t }: { t: T }) {
  return (
    <div className="min-w-0">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm font-semibold text-ink">{t("previewRoomHeading")}</p>
        <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-700">{t("previewCompleted")}</span>
      </div>
      <div className="mt-3 grid grid-cols-3 gap-2">
        <RoomSketch variant="living" className="aspect-[4/3] w-full rounded-md" />
        <RoomSketch variant="living" className="aspect-[4/3] w-full rounded-md" />
        <RoomSketch variant="living" scuff className="aspect-[4/3] w-full rounded-md ring-2 ring-amber-400" />
      </div>
      <div className="mt-3 rounded-lg border border-slate-200 p-3">
        <p className="flex items-center gap-1.5 text-[11px] font-medium text-brand"><Icon name="sparkles" className="h-3.5 w-3.5" /> {t("previewAiReviewed")}</p>
        <p className="mt-1.5 text-xs leading-relaxed text-slate-600">{t("previewAiSample")}</p>
      </div>
      <div className="mt-2 flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-900">
        <Icon name="alert" className="h-4 w-4 shrink-0" /> <span className="truncate">{t("previewDefect")}</span>
      </div>
    </div>
  );
}

function ComparisonCard({ t }: { t: T }) {
  return (
    <div className="absolute -bottom-32 -left-8 hidden w-60 rounded-xl border border-slate-200 bg-white p-3 shadow-lg lg:block">
      <p className="text-xs font-semibold text-ink">{t("previewCompareTitle")}</p>
      <div className="mt-2 grid grid-cols-2 gap-2">
        <figure><RoomSketch variant="kitchen" className="aspect-[4/3] w-full rounded" /><figcaption className="mt-1 text-[10px] text-slate-500">{t("previewMoveIn")}</figcaption></figure>
        <figure><RoomSketch variant="kitchen" scuff className="aspect-[4/3] w-full rounded" /><figcaption className="mt-1 text-[10px] text-slate-500">{t("previewMoveOut")}</figcaption></figure>
      </div>
      <p className="mt-2 inline-flex rounded-full bg-amber-100 px-2 py-0.5 text-[11px] font-medium text-amber-800">{t("previewDecision")}</p>
    </div>
  );
}

function ReportCard({ t }: { t: T }) {
  return (
    <div className="absolute -right-4 -top-6 hidden items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-lg sm:flex">
      <span className="grid h-9 w-9 place-items-center rounded-lg bg-brand-50 text-brand"><Icon name="file" className="h-5 w-5" /></span>
      <span>
        <span className="block text-xs font-semibold text-ink">{t("previewReportTitle")}</span>
        <span className="block text-[11px] text-slate-500">{t("previewReportStatus")}</span>
      </span>
    </div>
  );
}
