import { Icon, type IconName } from "./icons";
import { SectionHeading } from "./SectionHeading";

// Only features that exist in the product today (see README → "How to test the flows").
const FEATURES: { icon: IconName; title: string; text: string }[] = [
  { icon: "building", title: "Property management", text: "Properties with their own room layouts, tenancies and tenant invitations." },
  { icon: "doorIn", title: "Move In inspections", text: "A complete baseline of the property at the start of the tenancy." },
  { icon: "doorOut", title: "Move Out inspections", text: "Recorded against the Move In report of the same tenancy." },
  { icon: "rooms", title: "Room by room", text: "Rooms are snapshotted per inspection and completed one by one." },
  { icon: "camera", title: "Photo documentation", text: "Camera uploads with progress and retry, resized on the device." },
  { icon: "sparkles", title: "AI-assisted descriptions", text: "Drafted from the photos, labelled as AI and always editable." },
  { icon: "alert", title: "Defect documentation", text: "Each defect with photos, location, classification and confirmation." },
  { icon: "compare", title: "Move In vs Move Out", text: "Side-by-side record and a comparison decision for every room." },
  { icon: "file", title: "Professional PDF reports", text: "Immutable, versioned reports with PDF download and share links." },
  { icon: "userCheck", title: "Tenant review", text: "Tenants add observations, then accept or dispute the report." },
  { icon: "history", title: "Inspection history", text: "Every inspection and report of a property kept in one place." },
];

export function FeaturesSection() {
  return (
    <section id="features" aria-labelledby="features-title" className="scroll-mt-20 border-t border-slate-200 bg-slate-50/70 py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading id="features-title" eyebrow="Features" title="Everything an inspection needs, nothing it does not"
          intro="Built around the real inspection workflow: from the first photo to the tenant's confirmation." />
        <dl className="mt-12 grid gap-x-8 gap-y-8 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((f) => (
            <div key={f.title} className="flex gap-4">
              <dt className="shrink-0">
                <span className="grid h-10 w-10 place-items-center rounded-lg border border-slate-200 bg-white text-brand shadow-sm">
                  <Icon name={f.icon} className="h-5 w-5" />
                </span>
              </dt>
              <dd>
                <p className="font-semibold text-ink">{f.title}</p>
                <p className="mt-1 text-sm leading-relaxed text-slate-600">{f.text}</p>
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </section>
  );
}
