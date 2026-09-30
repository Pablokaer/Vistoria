import { Icon, type IconName } from "./icons";
import { SectionHeading } from "./SectionHeading";

const STEPS: { icon: IconName; who: string; title: string; text: string }[] = [
  { icon: "building", who: "Company", title: "Create the property and request an inspection", text: "Rooms, tenancy and a Move In or Move Out inspection — published publicly or sent privately." },
  { icon: "clipboard", who: "Inspector", title: "Accept and start the inspection", text: "The inspection is assigned to one inspector and opens on their phone." },
  { icon: "camera", who: "Inspector", title: "Add photos to every room", text: "Room photos and defect photos on a single page, uploaded as you go." },
  { icon: "sparkles", who: "AI", title: "AI drafts the descriptions", text: "Room, defect and Move Out comparison texts are drafted from the photos — clearly labelled." },
  { icon: "check", who: "Inspector", title: "Review, edit and finalize", text: "Every text is editable; defects are confirmed by the inspector before finalizing." },
  { icon: "file", who: "Platform", title: "A professional report is generated", text: "An immutable, versioned report with a PDF is frozen at finalization." },
  { icon: "userCheck", who: "Tenant", title: "The tenant reviews and confirms", text: "Observations per room, then accept the report or explain a disagreement." },
];

export function HowItWorksSection() {
  return (
    <section id="how-it-works" aria-labelledby="how-title" className="scroll-mt-20 bg-white py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="how-title" eyebrow="How it works" title="From request to signed-off report in seven steps" centered />
        <ol className="mt-14 grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((s, i) => (
            <li key={s.title} className="relative">
              <div className="flex items-center gap-3">
                <span className="grid h-10 w-10 shrink-0 place-items-center rounded-full bg-brand text-sm font-semibold text-white">{i + 1}</span>
                <span className="inline-flex items-center gap-1.5 rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600">
                  <Icon name={s.icon} className="h-3.5 w-3.5" />{s.who}
                </span>
              </div>
              <h3 className="mt-4 text-base font-semibold text-ink">{s.title}</h3>
              <p className="mt-1.5 text-sm leading-relaxed text-slate-600">{s.text}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
