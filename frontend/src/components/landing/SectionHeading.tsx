/** Eyebrow + title + intro used by every landing section. Example: `<SectionHeading eyebrow="Pricing" title="…" />` */
export function SectionHeading({ id, eyebrow, title, intro, centered }: { id?: string; eyebrow: string; title: string; intro?: string; centered?: boolean }) {
  return (
    <div className={centered ? "mx-auto max-w-2xl text-center" : "max-w-2xl"}>
      <p className="text-sm font-semibold text-brand">{eyebrow}</p>
      <h2 id={id} className="mt-2 text-3xl font-semibold tracking-tight text-ink sm:text-4xl">{title}</h2>
      {intro && <p className="mt-4 text-lg leading-relaxed text-ink-3">{intro}</p>}
    </div>
  );
}
