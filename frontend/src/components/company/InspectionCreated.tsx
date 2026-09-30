"use client";

import { Icon } from "@/components/icons";
import { Card, CopyField, InspectionTypeTag, LinkButton, Notice, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
import type { PublishResult } from "@/lib/types";

function InvitationDetails({ invitation }: { invitation: NonNullable<PublishResult["invitation"]> }) {
  const t = useT(companyFormMessages);
  const { formatDate } = useFormatters();
  return (
    <div className="space-y-4">
      <Notice tone="warning">{t("copyNowWarning")}</Notice>
      <CopyField label={t("invitationLink")} value={invitation.link} />
      <div>
        <span className="mb-1.5 block text-label font-medium text-ink-2">{t("accessCode")}</span>
        <div className="tabular rounded-md bg-sidebar px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{invitation.accessCode}</div>
        <p className="mt-1.5 text-caption text-ink-3">{t("codeValidity", { date: formatDate(invitation.expiresAt), attempts: invitation.maxAttempts })}</p>
      </div>
    </div>
  );
}

/** Shown once after creating an inspection (a private invitation's code can never be displayed again). */
export function InspectionCreated({ result, extra }: { result: PublishResult; extra?: React.ReactNode }) {
  const t = useT(companyFormMessages);
  const i = result.inspection;
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
        {result.invitation ? <InvitationDetails invitation={result.invitation} /> : (
          <p className="text-body text-ink-2">{i.status === "Open" ? t("openInMarketplace", { rooms: i.rooms.length }) : t("publishFromInspectionPage")}</p>
        )}
        {result.invitation && <p className="mt-4 text-label text-ink-3">{t("privateNextStep")}</p>}
        <div className="mt-5 flex flex-wrap gap-2 border-t border-line pt-4">
          <LinkButton href={`/company/inspections/${i.id}`}>{t("goToInspection")}</LinkButton>{extra}
        </div>
      </Card>
    </div>
  );
}
