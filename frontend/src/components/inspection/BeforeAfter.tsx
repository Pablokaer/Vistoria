"use client";

import type { ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { inspectionToolsMessages } from "@/i18n/messages/inspectionTools";
import { cx } from "@/lib/cx";
import { PhotoGallery } from "./PhotoGallery";

export interface ConditionSide { photos: string[]; text: string | null; meta?: string; extra?: ReactNode }

function SideHeader({ icon, label, meta, tone }: { icon: IconName; label: string; meta?: string; tone: "movein" | "moveout" }) {
  return (
    <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
      <span className={cx("inline-flex items-center gap-1.5 rounded-sm px-2 py-0.5 text-caption font-semibold",
        tone === "movein" ? "bg-movein-50 text-movein" : "bg-moveout-50 text-moveout")}>
        <Icon name={icon} className="h-3.5 w-3.5" />{label}
      </span>
      {meta && <span className="text-caption text-ink-3">{meta}</span>}
    </div>
  );
}

function Side({ side, header, conditionLabel }: { side: ConditionSide; header: ReactNode; conditionLabel: string }) {
  const t = useT(inspectionToolsMessages);
  return (
    <div className="min-w-0 rounded-md bg-surface-2 p-3 ring-1 ring-inset ring-line">
      {header}
      <PhotoGallery urls={side.photos} size="sm" />
      <p className="mt-3 text-caption font-semibold uppercase tracking-wide text-ink-3">{conditionLabel}</p>
      <p className="mt-0.5 whitespace-pre-line text-label text-ink-2">{side.text?.trim() || t("noCurrentDescription")}</p>
      {side.extra}
    </div>
  );
}

/**
 * Move In (before) next to Move Out (after): side by side from lg, stacked on phones.
 * Example: `<BeforeAfter before={{ photos, text, meta }} after={{ photos, text }} />`
 */
export function BeforeAfter({ before, after }: { before: ConditionSide; after: ConditionSide }) {
  const t = useT(inspectionToolsMessages);
  return (
    <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
      <Side side={before} conditionLabel={t("previousCondition")}
        header={<SideHeader icon="doorIn" label={t("beforeMoveIn")} meta={before.meta} tone="movein" />} />
      <Side side={after} conditionLabel={t("currentCondition")}
        header={<SideHeader icon="doorOut" label={t("afterMoveOut")} meta={after.meta} tone="moveout" />} />
    </div>
  );
}
