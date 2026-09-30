import { Icon, type IconName } from "./icons";
import { SectionHeading } from "./SectionHeading";

const AUDIENCES: { icon: IconName; title: string; text: string; benefits: string[] }[] = [
  {
    icon: "building", title: "Letting agencies & property managers",
    text: "Keep every property, tenancy and inspection in one workspace.",
    benefits: ["Publish inspections to a marketplace or invite a private inspector", "Track progress room by room", "One source of truth when a tenancy ends"],
  },
  {
    icon: "phone", title: "Inspectors",
    text: "A mobile workflow that removes the typing from the visit.",
    benefits: ["Photograph every room and defect on one page", "AI drafts the descriptions, you review and edit", "Move In record at hand during the Move Out"],
  },
  {
    icon: "userCheck", title: "Tenants",
    text: "A clear record of the property they are moving into or out of.",
    benefits: ["Read the report online or as a PDF", "Add observations per room", "Confirm the report or raise a disagreement"],
  },
];

export function AudienceSection() {
  return (
    <section aria-labelledby="audience-title" className="border-y border-slate-200 bg-slate-50/70 py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="audience-title" eyebrow="Who it is for" title="One record everyone involved in a tenancy can trust"
          intro="Companies order the inspection, inspectors carry it out and tenants review the result — each with a view designed for their part." />
        <div className="mt-12 grid gap-6 md:grid-cols-3">
          {AUDIENCES.map((a) => (
            <article key={a.title} className="rounded-2xl border border-slate-200 bg-white p-6">
              <span className="grid h-10 w-10 place-items-center rounded-lg bg-brand-50 text-brand"><Icon name={a.icon} className="h-5 w-5" /></span>
              <h3 className="mt-4 text-lg font-semibold text-ink">{a.title}</h3>
              <p className="mt-1 text-sm text-slate-600">{a.text}</p>
              <ul className="mt-4 space-y-2">
                {a.benefits.map((b) => (
                  <li key={b} className="flex gap-2 text-sm text-slate-700"><Icon name="check" className="mt-0.5 h-4 w-4 shrink-0 text-brand" strokeWidth={2.5} />{b}</li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
