"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { InspectionTypeTag, ProgressBar, PropertyThumb, StatusBadge, VisibilityTag } from "@/components/ui";
import { cx } from "@/lib/cx";
import type { InspectionType, Visibility } from "@/lib/types";

export interface InspectionFact { icon: IconName; text: string }

/**
 * What an inspection card shows, independent of the API type it came from (see inspectionCardModels.ts).
 * Only real data goes in here: facts are omitted when the API has no value.
 */
export interface InspectionCardModel {
  href: string;
  type: InspectionType;
  title: string;
  subtitle?: string;
  status?: string;
  visibility?: Visibility;
  facts: InspectionFact[];
  progress?: { value: number; total: number };
  cta: string;
  tags?: ReactNode;
}

/**
 * One card for every inspection list (marketplace, my inspections, company recent). The whole card is the link;
 * the CTA text tells what happens next ("Continue inspection", "View report"…).
 * Example: `<InspectionCard model={fromSummary(item, t, fmt)} />`
 */
export function InspectionCard({ model, className, testId }: { model: InspectionCardModel; className?: string; testId?: string }) {
  return (
    <Link href={model.href} data-testid={testId}
      className={cx("group flex h-full flex-col rounded-lg border border-line bg-surface p-4 shadow-card transition hover:-translate-y-px hover:border-line-strong hover:shadow-raised", className)}>
      <div className="flex items-start gap-3">
        <PropertyThumb type={model.type} />
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex flex-wrap items-center gap-1.5">
            <InspectionTypeTag type={model.type} />
            {model.visibility && <VisibilityTag visibility={model.visibility} />}
            {model.tags}
          </div>
          <h3 className="line-clamp-2 text-card-title font-semibold text-ink">{model.title}</h3>
          {model.subtitle && <p className="truncate text-label text-ink-3">{model.subtitle}</p>}
        </div>
        {model.status && <StatusBadge value={model.status} />}
      </div>
      {model.facts.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-x-4 gap-y-1.5 text-label text-ink-3">
          {model.facts.map((f) => (
            <li key={f.text} className="inline-flex items-center gap-1.5"><Icon name={f.icon} className="h-4 w-4 text-ink-4" />{f.text}</li>
          ))}
        </ul>
      )}
      {model.progress && model.progress.total > 0 && <div className="mt-3"><ProgressBar value={model.progress.value} total={model.progress.total} /></div>}
      <div className="mt-auto flex items-center justify-end pt-3">
        <span className="inline-flex items-center gap-1 text-label font-semibold text-brand">
          {model.cta}<Icon name="arrowRight" className="h-4 w-4 transition group-hover:translate-x-0.5" />
        </span>
      </div>
    </Link>
  );
}

/** Grid for cards: 1 column on phones, 2 on tablets, 3 on wide screens when `dense`. */
export function InspectionCardGrid({ children, dense }: { children: ReactNode; dense?: boolean }) {
  return <div className={cx("grid gap-3 sm:grid-cols-2", dense && "xl:grid-cols-3")}>{children}</div>;
}
