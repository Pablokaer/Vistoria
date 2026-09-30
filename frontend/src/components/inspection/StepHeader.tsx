"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { Icon } from "@/components/icons";
import { InspectionTypeTag, ProgressBar } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { cx } from "@/lib/cx";
import type { InspectionDetails } from "@/lib/types";

export type ExecutionStep = 1 | 2 | 3;

function Steps({ current }: { current: ExecutionStep }) {
  const t = useT(captureMessages);
  const labels = [t("stepPhotos"), t("stepDescriptions"), t("stepReview")];
  return (
    <ol aria-label={t("stepsLabel")} className="flex items-center gap-1.5 text-caption font-medium">
      {labels.map((label, i) => {
        const step = (i + 1) as ExecutionStep;
        const done = step < current;
        const active = step === current;
        return (
          <li key={label} aria-current={active ? "step" : undefined} className="flex items-center gap-1.5">
            {i > 0 && <span aria-hidden="true" className={cx("h-px w-4 sm:w-6", done || active ? "bg-brand" : "bg-line-strong")} />}
            <span className={cx("grid h-5 w-5 place-items-center rounded-full text-[11px] font-semibold",
              active ? "bg-brand text-white" : done ? "bg-brand-50 text-brand" : "bg-neutral-50 text-ink-3 ring-1 ring-inset ring-line")}>
              {done ? <Icon name="check" className="h-3 w-3" /> : step}
            </span>
            <span className={cx(active ? "text-ink" : "text-ink-3", !active && "hidden sm:inline")}>{label}</span>
          </li>
        );
      })}
    </ol>
  );
}

/**
 * Sticky header of the execution flow: where you are (property, step), how far you are (progress) and whether
 * your work is saved. On phones it sits under the app top bar and carries the room chip strip.
 */
export function StepHeader({ inspection, step, title, progress, status, actions, rooms }: {
  inspection: InspectionDetails; step: ExecutionStep; title: string;
  progress: { value: number; total: number; label: string }; status?: ReactNode; actions?: ReactNode; rooms?: ReactNode;
}) {
  const t = useT(captureMessages);
  const address = [inspection.property.addressLine1, inspection.property.city].filter(Boolean).join(", ");
  return (
    <div className="sticky top-14 z-20 -mx-4 mb-5 border-b border-line bg-canvas/95 px-4 pb-3 pt-3 backdrop-blur sm:-mx-6 sm:px-6 lg:top-0 lg:-mx-8 lg:px-8 lg:pt-4">
      <div className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2">
        <div className="min-w-0">
          <Link href={`/agent/inspections/${inspection.id}`} className="mb-1 inline-flex max-w-full items-center gap-1 text-caption font-medium text-ink-3 hover:text-brand">
            <Icon name="arrowLeft" className="h-3.5 w-3.5 shrink-0" /><span className="truncate">{address || t("inspection")}</span>
          </Link>
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-section font-semibold text-ink sm:text-page">{title}</h1>
            <InspectionTypeTag type={inspection.inspectionType} />
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-3"><Steps current={step} />{actions}</div>
      </div>
      <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1">
        <div className="min-w-48 flex-1"><ProgressBar value={progress.value} total={progress.total} label={progress.label} /></div>
        {status}
      </div>
      {rooms && <div className="mt-3 lg:hidden">{rooms}</div>}
    </div>
  );
}
