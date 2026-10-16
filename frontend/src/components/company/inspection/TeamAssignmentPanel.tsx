"use client";

import { useState } from "react";
import { useTeamAgents } from "@/components/company/team/TeamAgentPicker";
import { Button, Card, ErrorBanner, Field, Select, useToast } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyTeamMessages } from "@/i18n/messages/company-team";
import { errorMessage, put } from "@/lib/api";
import type { InspectionDetails } from "@/lib/types";

/**
 * Assign a Team inspection to one agent, switch agents, or release it back to the whole team (until it starts).
 * Shown when the API allows "assign". Example: `<TeamAssignmentPanel inspection={i} onChange={setData} />`
 */
export function TeamAssignmentPanel({ inspection: i, onChange }: { inspection: InspectionDetails; onChange: (d: InspectionDetails) => void }) {
  const t = useT(companyTeamMessages);
  const toast = useToast();
  const team = useTeamAgents();
  const [agentId, setAgentId] = useState(i.agent?.id ?? "");
  const [busy, setBusy] = useState<"assign" | "release" | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function save(kind: "assign" | "release") {
    setBusy(kind);
    setError(null);
    try {
      const updated = await put<InspectionDetails>(`/api/inspections/${i.id}/assignment`, { agentId: kind === "assign" ? agentId : null });
      onChange(updated);
      setAgentId(updated.agent?.id ?? "");
      toast.notify({ tone: "success", title: kind === "assign" ? t("assignedToast") : t("releasedToast") });
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(null);
    }
  }

  return (
    <Card title={t("assignmentTitle")} description={t("assignmentHint")}>
      <ErrorBanner message={error ?? team.error} />
      <Field label={t("whoInspects")}>
        <Select value={agentId} onChange={(e) => setAgentId(e.target.value)} data-testid="assign-agent">
          <option value="">{t("selectAgent")}</option>
          {team.data?.map((a) => <option key={a.agentId} value={a.agentId}>{a.fullName}</option>)}
        </Select>
      </Field>
      <div className="mt-4 flex flex-wrap justify-end gap-2 border-t border-line pt-4">
        {i.allowedActions.includes("release") && (
          <Button variant="secondary" loading={busy === "release"} onClick={() => void save("release")}>{t("releaseToTeam")}</Button>
        )}
        <Button icon="userCheck" loading={busy === "assign"} disabled={!agentId || agentId === i.agent?.id} onClick={() => void save("assign")}>{t("assign")}</Button>
      </div>
    </Card>
  );
}
