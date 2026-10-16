"use client";

import Link from "next/link";
import { Field, Notice, Select } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyTeamMessages } from "@/i18n/messages/company-team";
import { useApi } from "@/lib/hooks";
import type { TeamAgent } from "@/lib/types";

/** The company's registered agents (GET /api/companies/me/agents). Example: `const team = useTeamAgents();` */
export function useTeamAgents() {
  return useApi<TeamAgent[]>("/api/companies/me/agents");
}

/**
 * "Who does it" for a Team inspection: the whole team (empty value) or one agent, who gets it without accepting.
 * Example: `<TeamAgentPicker value={agentId} onChange={setAgentId} />`
 */
export function TeamAgentPicker({ value, onChange }: { value: string; onChange: (agentId: string) => void }) {
  const t = useT(companyTeamMessages);
  const team = useTeamAgents();
  if (team.data?.length === 0) {
    return <Notice>{t("teamEmpty")} <Link className="font-medium underline" href="/company/agents">{t("registerAgents")}</Link></Notice>;
  }
  return (
    <Field label={t("whoInspects")} hint={value ? t("assignedHint") : t("anyAgentHint")}>
      <Select value={value} onChange={(e) => onChange(e.target.value)} data-testid="team-agent">
        <option value="">{t("anyTeamAgent")}</option>
        {team.data?.map((a) => <option key={a.agentId} value={a.agentId}>{a.fullName} · {a.email}</option>)}
      </Select>
    </Field>
  );
}
