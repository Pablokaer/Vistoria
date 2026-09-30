"use client";

import { Button, Card, CopyField, DefinitionList, Notice } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import type { InspectionDetails, PrivateInvitation } from "@/lib/types";

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

/** Status of the private link + code; regenerating shows the new pair once (see NewInvitationPanel). */
export function InvitationStatusPanel({ invitation, onRegenerate, busy }: { invitation: NonNullable<InspectionDetails["invitation"]>; onRegenerate?: () => void; busy: boolean }) {
  const t = useT(companyInspectionMessages);
  const { formatDateTime } = useFormatters();
  const status = invitation.used ? t("usedByAgent") : invitation.revoked ? t("revoked") : invitation.expired ? t("expired") : t("active");
  return (
    <Card title={t("privateInvitation")} actions={onRegenerate && <Button variant="ghost" size="sm" icon="refresh" loading={busy} onClick={onRegenerate}>{t("newLinkAndCode")}</Button>}>
      <DefinitionList items={[[t("status"), status], [t("expires"), formatDateTime(invitation.expiresAt)], [t("failedAttempts"), `${invitation.attemptCount} / ${invitation.maxAttempts}`]]} />
    </Card>
  );
}

/** A freshly generated private invitation: link + code, shown only this once. */
export function NewInvitationPanel({ invitation }: { invitation: PrivateInvitation }) {
  const t = useT(companyInspectionMessages);
  return (
    <Card className="mb-6 animate-fade-in" title={t("privateInvitation")}>
      <Notice tone="warning">{t("shownOnce")}</Notice>
      <div className="space-y-3">
        <CopyField label={t("invitationLink")} value={invitation.link} />
        <div className="tabular rounded-md bg-sidebar px-4 py-3 text-center font-mono text-3xl tracking-[0.4em] text-white">{invitation.accessCode}</div>
      </div>
    </Card>
  );
}
