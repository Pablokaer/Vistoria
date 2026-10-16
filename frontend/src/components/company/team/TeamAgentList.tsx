"use client";

import { Icon } from "@/components/icons";
import { Button, Card, EmptyState, useConfirm, useToast } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyTeamMessages } from "@/i18n/messages/company-team";
import { del, errorMessage } from "@/lib/api";
import type { TeamAgent } from "@/lib/types";

function TeamAgentRow({ agent, canManage, onRemove }: { agent: TeamAgent; canManage: boolean; onRemove: () => void }) {
  const t = useT(companyTeamMessages);
  const { formatDate } = useFormatters();
  return (
    <li className="flex flex-wrap items-center gap-3 px-4 py-3 sm:px-5" data-testid="team-agent-row">
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-brand-50 text-brand"><Icon name="user" className="h-5 w-5" /></span>
      <div className="min-w-0 flex-1">
        <p className="truncate font-medium text-ink">{agent.fullName}</p>
        <p className="truncate text-label text-ink-3">{agent.email}{agent.phone ? ` · ${agent.phone}` : ""}</p>
      </div>
      <div className="text-right text-caption text-ink-3">
        <p className="tabular">{t("activeCount", { count: agent.activeInspections })}</p>
        <p>{t("addedOn", { date: formatDate(agent.addedAt) })}</p>
      </div>
      {canManage && <Button variant="ghost" size="sm" icon="trash" onClick={onRemove}>{t("remove")}</Button>}
    </li>
  );
}

/**
 * The company's team with a remove action for Owner/Admin (the API enforces it again).
 * Example: `<TeamAgentList agents={team.data} canManage onChanged={team.reload} />`
 */
export function TeamAgentList({ agents, canManage, onChanged }: { agents: TeamAgent[]; canManage: boolean; onChanged: () => void }) {
  const t = useT(companyTeamMessages);
  const toast = useToast();
  const [confirm, dialog] = useConfirm();

  async function remove(agent: TeamAgent) {
    if (!(await confirm({ title: t("confirmRemove", { name: agent.fullName }), description: t("confirmRemoveHint"), confirmLabel: t("remove"), danger: true }))) return;
    try {
      await del(`/api/companies/me/agents/${agent.agentId}`);
      toast.notify({ tone: "success", title: t("removedToast", { name: agent.fullName }) });
      onChanged();
    } catch (e) {
      toast.notify({ tone: "error", title: errorMessage(e) });
    }
  }

  if (agents.length === 0) return <EmptyState icon="users" title={t("noAgentsTitle")}>{t("noAgentsBody")}</EmptyState>;
  return (
    <Card title={t("teamListTitle")} flush>
      {dialog}
      <ul className="divide-y divide-line">
        {agents.map((a) => <TeamAgentRow key={a.agentId} agent={a} canManage={canManage} onRemove={() => void remove(a)} />)}
      </ul>
    </Card>
  );
}
