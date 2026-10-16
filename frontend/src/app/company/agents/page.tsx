"use client";

import { RegisterAgentForm } from "@/components/company/team/RegisterAgentForm";
import { TeamAgentList } from "@/components/company/team/TeamAgentList";
import { useTeamAgents } from "@/components/company/team/TeamAgentPicker";
import { ErrorBanner, Notice, PageHeader, Skeleton } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyTeamMessages } from "@/i18n/messages/company-team";
import { useAuth } from "@/lib/auth";

// Owner and Admin may register/remove agents; the API enforces it (403) — this only hides pointless controls.
const TEAM_MANAGER_ROLES = ["Owner", "Admin"];

/** The company's own agents: register new ones (or link existing agent accounts) and remove them. */
export default function CompanyAgentsPage() {
  const { user } = useAuth();
  const t = useT(companyTeamMessages);
  const team = useTeamAgents();
  const company = user?.company;
  if (!company) return null;
  const canManage = TEAM_MANAGER_ROLES.includes(company.role);
  return (
    <div className="mx-auto max-w-page space-y-6">
      <PageHeader title={t("teamTitle")} subtitle={t("teamSubtitle", { company: company.companyName })} />
      <ErrorBanner message={team.error} onRetry={team.reload} />
      {canManage ? <RegisterAgentForm onRegistered={() => void team.reload()} /> : <Notice>{t("ownerAdminOnly")}</Notice>}
      {team.loading && !team.data ? <Skeleton className="h-32" /> : <TeamAgentList agents={team.data ?? []} canManage={canManage} onChanged={() => void team.reload()} />}
    </div>
  );
}
