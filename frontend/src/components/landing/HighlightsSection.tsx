import { Icon } from "./icons";
import { RoomSketch } from "./RoomSketch";
import { SectionHeading } from "./SectionHeading";

const AI_POINTS = [
  "Describes only what is visible in the photos — no assumptions, no blame",
  "Every AI text is labelled and stored apart from the inspector's final text",
  "Defects and comparison decisions are always confirmed by a person",
];

const REPORT_POINTS = [
  "Move Out rooms shown next to their Move In record",
  "Finalized reports are frozen, versioned and hashed",
  "PDF download, expiring share links and online tenant review",
];

function Points({ items }: { items: string[] }) {
  return (
    <ul className="mt-6 space-y-3">
      {items.map((p) => (
        <li key={p} className="flex gap-3 text-slate-700"><Icon name="check" className="mt-1 h-4 w-4 shrink-0 text-brand" strokeWidth={2.5} />{p}</li>
      ))}
    </ul>
  );
}

export function HighlightsSection() {
  return (
    <section aria-label="Highlights" className="bg-white py-20 sm:py-24">
      <div className="mx-auto grid max-w-6xl gap-20 px-4 sm:px-6">
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <div>
            <SectionHeading eyebrow="AI assistance" title="AI writes the first draft. The inspector has the final word." />
            <Points items={AI_POINTS} />
          </div>
          <AiDraftVisual />
        </div>
        <div className="grid items-center gap-10 lg:grid-cols-2">
          <ComparisonVisual />
          <div className="lg:order-first">
            <SectionHeading eyebrow="Move In vs Move Out" title="Compare the start and the end of a tenancy, then share a report nobody can alter" />
            <Points items={REPORT_POINTS} />
          </div>
        </div>
      </div>
    </section>
  );
}

function AiDraftVisual() {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5 sm:p-6">
      <div className="grid grid-cols-2 gap-3">
        <RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-lg" />
        <RoomSketch variant="bedroom" className="aspect-[4/3] w-full rounded-lg" />
      </div>
      <div className="mt-4 rounded-xl border border-slate-200 bg-white p-4">
        <p className="flex items-center gap-1.5 text-xs font-medium text-brand"><Icon name="sparkles" className="h-4 w-4" /> AI-drafted description</p>
        <p className="mt-2 text-sm leading-relaxed text-slate-600">
          Walls painted white and in good visible condition. Beige carpet with light wear near the door. Double-glazed window with curtain rail.
          No visible damage is apparent in the provided images.
        </p>
        <p className="mt-3 border-t border-slate-100 pt-3 text-xs text-slate-500">Edited and approved by the inspector before finalizing.</p>
      </div>
    </div>
  );
}

function ComparisonVisual() {
  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50 p-5 sm:p-6">
      <p className="text-sm font-semibold text-ink">Living room</p>
      <div className="mt-3 grid grid-cols-2 gap-3">
        <figure><RoomSketch variant="living" className="aspect-[4/3] w-full rounded-lg" /><figcaption className="mt-2 text-xs text-slate-500">Move In · 12 Jan 2026</figcaption></figure>
        <figure><RoomSketch variant="living" scuff className="aspect-[4/3] w-full rounded-lg" /><figcaption className="mt-2 text-xs text-slate-500">Move Out · 28 Sep 2026</figcaption></figure>
      </div>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3">
        <span className="text-sm text-slate-700">Scuff marks, left of window</span>
        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">Pre-existing</span>
      </div>
    </div>
  );
}
