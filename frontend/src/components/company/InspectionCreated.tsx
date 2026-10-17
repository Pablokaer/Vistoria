"use client";

import { Icon } from "@/components/icons";
import { Card, InspectionTypeTag, LinkButton, StatusBadge } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
import type { Translate } from "@/i18n/translate";
import type { InspectionDetails } from "@/lib/types";

type CompanyFormKey = keyof (typeof companyFormMessages)["en"];

/** What happened on publish: open to the whole team (Public) or assigned to one agent (Direct). */
function publishedMessage(i: InspectionDetails, t: Translate<CompanyFormKey>): string {
  if (i.status === "Draft") return t("publishFromInspectionPage");
  if (i.status === "Assigned" && i.agent) return t("assignedToAgent", { agent: i.agent.fullName, rooms: i.rooms.length });
  return t("openToTeam", { rooms: i.rooms.length });
}

/** Shown after creating an inspection. Example: `<InspectionCreated inspection={created} />` */
export function InspectionCreated({ inspection: i, extra }: { inspection: InspectionDetails; extra?: React.ReactNode }) {
  const t = useT(companyFormMessages);
  const published = i.status !== "Draft";
  return (
    <div className="mx-auto max-w-narrow animate-fade-in">
      <div className="mb-6 flex items-start gap-4">
        <span className="grid h-12 w-12 shrink-0 place-items-center rounded-full bg-success-50 text-success-700"><Icon name="checkCircle" className="h-7 w-7" /></span>
        <div className="min-w-0">
          <h1 className="text-page font-semibold tracking-tight text-ink">{published ? t("inspectionPublished") : t("draftSaved")}</h1>
          <div className="mt-1.5 flex flex-wrap items-center gap-2 text-body text-ink-3">
            <InspectionTypeTag type={i.inspectionType} /><StatusBadge value={i.status} /><span className="truncate">{i.property.addressLine1}, {i.property.city}</span>
          </div>
        </div>
      </div>
      <Card title={t("nextSteps")}>
        <p className="text-body text-ink-2">{publishedMessage(i, t)}</p>
        <div className="mt-5 flex flex-wrap gap-2 border-t border-line pt-4">
          <LinkButton href={`/company/inspections/${i.id}`}>{t("goToInspection")}</LinkButton>{extra}
        </div>
      </Card>
    </div>
  );
}
