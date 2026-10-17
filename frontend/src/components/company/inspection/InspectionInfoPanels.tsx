"use client";

import { Card, DefinitionList } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import type { InspectionDetails } from "@/lib/types";

/** Who inspects, when, against what, and the company's instructions. */
export function InspectionDetailsPanel({ inspection: i }: { inspection: InspectionDetails }) {
  const t = useT(companyInspectionMessages);
  const { formatDate, formatDateTime, humanize } = useFormatters();
  const items: [string, React.ReactNode][] = [
    [t("agent"), i.agent ? `${i.agent.fullName}${i.agent.phone ? ` · ${i.agent.phone}` : ""}` : t("notAssigned")],
    [t("visibility"), humanize(i.visibility)],
    [t("scheduled"), formatDate(i.scheduledDate)],
  ];
  if (i.acceptBy) items.push([t("acceptBy"), formatDateTime(i.acceptBy)]);
  if (i.comparisonInspection) items.push([t("comparedWith"), t("comparedWithValue", { reportNumber: i.comparisonInspection.reportNumber ?? "" })]);
  return (
    <Card title={t("details")}>
      <DefinitionList items={items} />
      {i.instructions && <p className="mt-4 whitespace-pre-line rounded-md bg-surface-2 p-3 text-body text-ink-2">{i.instructions}</p>}
    </Card>
  );
}

export function InspectionTenancyPanel({ tenancy }: { tenancy: NonNullable<InspectionDetails["tenancy"]> }) {
  const t = useT(companyInspectionMessages);
  const { formatDate } = useFormatters();
  return (
    <Card title={t("tenancy")} description={`${formatDate(tenancy.startDate)} – ${tenancy.endDate ? formatDate(tenancy.endDate) : t("ongoing")}`}>
      <ul className="space-y-1.5 text-body">
        {tenancy.members.map((m) => (
          <li key={m.id} className="flex justify-between gap-2"><span className="font-medium text-ink">{m.fullName}</span>
            <span className={m.joined ? "text-caption text-success-700" : "text-caption text-warning-700"}>{m.joined ? t("joinedLower") : t("invitedLower")}</span></li>
        ))}
        {tenancy.members.length === 0 && <li className="text-label text-ink-3">{t("noTenantsWaiting")}</li>}
      </ul>
    </Card>
  );
}
