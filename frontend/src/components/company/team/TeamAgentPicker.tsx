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
 * What the empty choice means: the whole team for a Team inspection, or an outside inspector with link + code for a
 * Private one (ADR 0021). Picking an agent works the same in both: they get the inspection without accepting it.
 */
export type TeamAgentPickerMode = "team" | "private";

const PICKER_TEXT = {
  team: { label: "whoInspects", empty: "anyTeamAgent", emptyHint: "anyAgentHint", assignedHint: "assignedHint" },
  private: { label: "sendTo", empty: "outsideInspector", emptyHint: "outsideInspectorHint", assignedHint: "privateAssignedHint" },
} as const;

/**
 * "Who does it": one of the company's agents, who gets it without accepting, or the mode's empty choice.
 * Example: `<TeamAgentPicker mode="private" value={agentId} onChange={setAgentId} />`
 */
export function TeamAgentPicker({ value, onChange, mode = "team" }: { value: string; onChange: (agentId: string) => void; mode?: TeamAgentPickerMode }) {
  const t = useT(companyTeamMessages);
  const team = useTeamAgents();
  const text = PICKER_TEXT[mode];
  if (team.data?.length === 0) {
    return <Notice>{t("teamEmpty")} <Link className="font-medium underline" href="/company/agents">{t("registerAgents")}</Link></Notice>;
  }
  return (
    <Field label={t(text.label)} hint={value ? t(text.assignedHint) : t(text.emptyHint)}>
      <Select value={value} onChange={(e) => onChange(e.target.value)} data-testid={`${mode}-agent`}>
        <option value="">{t(text.empty)}</option>
        {team.data?.map((a) => <option key={a.agentId} value={a.agentId}>{a.fullName} · {a.email}</option>)}
      </Select>
    </Field>
  );
}
