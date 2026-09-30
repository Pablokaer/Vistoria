// Adapters from API types to InspectionCardModel. Pure functions: translator and formatters are passed in.
import type { Formatters } from "@/i18n/formatting";
import type { InspectionListKey } from "@/i18n/messages/inspection-lists";
import type { Translate } from "@/i18n/translate";
import type { AvailableInspection, CompanyDashboard, InspectionStatus, InspectionSummary } from "@/lib/types";
import type { InspectionCardModel, InspectionFact } from "./InspectionCard";

type T = Translate<InspectionListKey>;
type RecentInspection = CompanyDashboard["recentInspections"][number];

const fact = (icon: InspectionFact["icon"], text: string | null | undefined): InspectionFact[] => (text ? [{ icon, text }] : []);

/** What the agent does next for each status of their own inspection. */
export function agentCta(status: InspectionStatus, t: T): string {
  if (status === "Assigned") return t("ctaStart");
  if (status === "InProgress") return t("ctaContinue");
  if (status === "Review") return t("ctaReview");
  if (["Completed", "AwaitingTenant", "Accepted", "Disputed"].includes(status)) return t("ctaReport");
  return t("ctaOpen");
}

/** Marketplace / invitation preview card. Example: `fromAvailable(item, t, formatters)`. */
export function fromAvailable(item: AvailableInspection, t: T, f: Formatters): InspectionCardModel {
  return {
    href: `/agent/available/${item.id}`,
    type: item.inspectionType,
    title: `${f.humanize(item.propertyType)} · ${item.city} ${item.postcodeArea}`.trim(),
    subtitle: item.companyName,
    visibility: item.visibility,
    facts: [
      { icon: "rooms", text: t("rooms", { count: item.roomCount }) },
      ...fact("calendar", item.scheduledDate && t("scheduled", { date: f.formatDate(item.scheduledDate) })),
      ...fact("clock", item.acceptBy && t("acceptBy", { date: f.formatDate(item.acceptBy) })),
      ...fact("compare", item.hasComparison ? t("withBaseline") : null),
    ],
    cta: t("ctaView"),
  };
}

/** The agent's own inspection (dashboard, My inspections, Completed). */
export function fromSummary(item: InspectionSummary, t: T, f: Formatters): InspectionCardModel {
  const done = item.completedAt !== null;
  return {
    href: `/agent/inspections/${item.id}`,
    type: item.inspectionType,
    title: item.propertyAddress,
    status: item.status,
    visibility: item.visibility,
    facts: [
      { icon: "rooms", text: t("rooms", { count: item.roomsTotal }) },
      ...fact("calendar", item.scheduledDate && t("scheduled", { date: f.formatDate(item.scheduledDate) })),
      done ? { icon: "checkCircle", text: t("completedOn", { date: f.formatDate(item.completedAt) }) }
        : { icon: "clock", text: t("updated", { date: f.formatDate(item.updatedAt) }) },
    ],
    progress: done ? undefined : { value: item.roomsCompleted, total: item.roomsTotal },
    cta: agentCta(item.status, t),
  };
}

/** A company's recent inspection (dashboard). */
export function fromCompanyRecent(item: RecentInspection, t: T, f: Formatters): InspectionCardModel {
  return {
    href: `/company/inspections/${item.id}`,
    type: item.inspectionType,
    title: item.propertyAddress,
    status: item.status,
    facts: [
      { icon: "user", text: item.agentName ?? t("noAgentYet") },
      { icon: "clock", text: t("updated", { date: f.formatDateTime(item.updatedAt) }) },
    ],
    cta: t("ctaView"),
  };
}
