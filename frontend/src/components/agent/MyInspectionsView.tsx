"use client";

import { useRouter } from "next/navigation";
import { InspectionCard, InspectionCardGrid } from "@/components/inspection/InspectionCard";
import { fromSummary } from "@/components/inspection/inspectionCardModels";
import { EmptyState, ErrorBanner, LinkButton, PageHeader, Skeleton, Tabs } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { inspectionListMessages } from "@/i18n/messages/inspection-lists";
import { useApi } from "@/lib/hooks";
import type { InspectionStatus, InspectionSummary } from "@/lib/types";

export type MyTab = "assigned" | "inProgress" | "review" | "completed";
export const MY_TABS: MyTab[] = ["assigned", "inProgress", "review", "completed"];

const TAB_STATUSES: Record<Exclude<MyTab, "completed">, InspectionStatus[]> = {
  assigned: ["Assigned"], inProgress: ["InProgress"], review: ["Review"],
};

const TAB_LABEL = { assigned: "tabAssigned", inProgress: "tabInProgress", review: "tabReview", completed: "tabCompleted" } as const;
const TAB_EMPTY = { assigned: "emptyAssigned", inProgress: "emptyInProgress", review: "emptyReview", completed: "noCompleted" } as const;

function itemsFor(tab: MyTab, active: InspectionSummary[], completed: InspectionSummary[]): InspectionSummary[] {
  if (tab === "completed") return [...completed].sort((a, b) => (b.completedAt ?? "").localeCompare(a.completedAt ?? ""));
  return active.filter((i) => TAB_STATUSES[tab].includes(i.status)).sort((a, b) => b.updatedAt.localeCompare(a.updatedAt));
}

/** Where a tab lives: Completed keeps its own route (/agent/completed), the rest use ?tab= on /agent/inspections. */
export function tabHref(tab: MyTab): string {
  return tab === "completed" ? "/agent/completed" : `/agent/inspections?tab=${tab}`;
}

// Without an explicit tab, open the stage most likely to need action.
function defaultTab(active: InspectionSummary[] | null): MyTab {
  const has = (s: InspectionStatus) => !!active?.some((i) => i.status === s);
  return has("InProgress") ? "inProgress" : has("Review") ? "review" : has("Assigned") ? "assigned" : "inProgress";
}

/**
 * The inspector's own work split by stage, with counts, so "what needs action" is visible without opening pages.
 * Example: `<MyInspectionsView tab="assigned" />` (omit `tab` to pick the first stage with work)
 */
export function MyInspectionsView({ tab: requested }: { tab?: MyTab }) {
  const t = useT(agentMessages);
  const cardText = useT(inspectionListMessages);
  const formatters = useFormatters();
  const router = useRouter();
  const active = useApi<InspectionSummary[]>("/api/agent/inspections?scope=active");
  const completed = useApi<InspectionSummary[]>("/api/agent/inspections?scope=completed");
  const tab = requested ?? defaultTab(active.data);
  const lists = { active: active.data ?? [], completed: completed.data ?? [] };
  const loaded = tab === "completed" ? completed.data : active.data;
  const items = itemsFor(tab, lists.active, lists.completed);
  const counts = (x: MyTab) => (x === "completed" ? completed.data?.length : active.data ? itemsFor(x, lists.active, []).length : undefined);
  const select = (next: MyTab) => { if (next !== tab) router.replace(tabHref(next), { scroll: false }); };
  return (
    <>
      <PageHeader title={t("myInspectionsTitle")} subtitle={t("myInspectionsSubtitleTabs")}
        actions={<LinkButton href="/agent/available" variant="secondary" icon="search">{t("findInspections")}</LinkButton>} />
      <ErrorBanner message={active.error ?? completed.error} onRetry={() => { void active.reload(); void completed.reload(); }} />
      <Tabs label={t("myInspectionsTitle")} value={tab} onChange={select} items={MY_TABS.map((x) => ({ value: x, label: t(TAB_LABEL[x]), count: counts(x) }))} />
      <div className="mt-5">
        {!loaded ? <InspectionCardGrid dense>{[0, 1, 2].map((i) => <Skeleton key={i} className="h-44" />)}</InspectionCardGrid>
          : items.length === 0 ? <EmptyState icon={tab === "completed" ? "checkCircle" : "inspections"} title={t(TAB_EMPTY[tab])}
            action={tab === "assigned" ? <LinkButton href="/agent/available" icon="search">{t("findInspections")}</LinkButton> : undefined} />
          : <InspectionCardGrid dense>{items.map((i) => <InspectionCard key={i.id} model={fromSummary(i, cardText, formatters)} />)}</InspectionCardGrid>}
      </div>
    </>
  );
}
