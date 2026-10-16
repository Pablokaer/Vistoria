import { getServerTranslator } from "@/i18n/server";
import { landingSectionMessages, type LandingSectionKey } from "@/i18n/messages/landing-sections";
import { cx } from "@/lib/cx";
import { EYEBROW, LANDING_CONTAINER, SECTION_TITLE } from "./landingStyles";

interface WorkflowStep { role: LandingSectionKey; title: LandingSectionKey; text: LandingSectionKey }

// The real inspection lifecycle (Draft → Open → Assigned → InProgress → Review → Completed → tenant decision),
// grouped by who acts.
const STEPS: WorkflowStep[] = [
  { role: "roleAgency", title: "step1Title", text: "step1Text" },
  { role: "roleInspector", title: "step2Title", text: "step2Text" },
  { role: "roleInspectorAi", title: "step3Title", text: "step3Text" },
  { role: "roleTenant", title: "step4Title", text: "step4Text" },
];

/** "How it works": the four hand-offs of an inspection, laid out as one ruled ledger rather than cards. */
export async function WorkflowLedger() {
  const t = await getServerTranslator(landingSectionMessages);
  return (
    <section id="workflow" aria-labelledby="workflow-title" className="scroll-mt-16 py-20 sm:py-28">
      <div className={LANDING_CONTAINER}>
        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] lg:items-end">
          <div>
            <p className={EYEBROW}>{t("workflowEyebrow")}</p>
            <h2 id="workflow-title" className={cx(SECTION_TITLE, "mt-4")}>{t("workflowTitle")}</h2>
          </div>
          <p className="max-w-[32rem] text-[1.0625rem] leading-relaxed text-ink-2 lg:justify-self-end">{t("workflowIntro")}</p>
        </div>
        <ol className="mt-14 grid border-t-2 border-ink sm:grid-cols-2 lg:grid-cols-4">
          {STEPS.map((step, index) => (
            <li key={step.title} className={cx("border-b border-rule py-7 sm:pr-8 lg:border-b-0 lg:pl-6 lg:first:pl-0",
              index > 0 && "lg:border-l", index % 2 === 1 && "sm:border-l sm:pl-6 lg:pl-6")}>
              <div className="flex items-baseline justify-between gap-3">
                <span className="font-mono text-sm text-paper-muted">{String(index + 1).padStart(2, "0")}</span>
                <span className="text-[0.6875rem] font-medium uppercase tracking-[0.12em] text-paper-muted">{t(step.role)}</span>
              </div>
              <h3 className="mt-5 font-display text-[1.75rem] leading-none text-ink">{t(step.title)}</h3>
              <p className="mt-3 text-[0.9375rem] leading-relaxed text-ink-2">{t(step.text)}</p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
