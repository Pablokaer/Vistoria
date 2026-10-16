import { cx } from "@/lib/cx";
import { EYEBROW, SECTION_INTRO, SECTION_TITLE } from "./landingStyles";

/**
 * Eyebrow + title (+ intro) opening a landing section; `id` labels the section (aria-labelledby).
 * Example: `<SectionHeading id="features-title" eyebrow="Features" title="Everything you need" intro="…" />`
 */
export function SectionHeading({ id, eyebrow, title, intro, centered }: {
  id: string; eyebrow: string; title: string; intro?: string; centered?: boolean;
}) {
  return (
    <div className={cx(centered && "mx-auto max-w-[44rem] text-center")}>
      <p className={EYEBROW}>{eyebrow}</p>
      <h2 id={id} className={cx(SECTION_TITLE, "mt-4")}>{title}</h2>
      {intro && <p className={cx(SECTION_INTRO, "mt-5", centered && "mx-auto")}>{intro}</p>}
    </div>
  );
}
