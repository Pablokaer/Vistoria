"use client";

import { useState } from "react";
import { Button, Card, ErrorBanner, Field, Input, useToast } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { companyTeamMessages } from "@/i18n/messages/company-team";
import { errorMessage, post } from "@/lib/api";
import type { TeamAgent } from "@/lib/types";

interface AgentDraft { fullName: string; email: string; phone: string; password: string }

const EMPTY_AGENT: AgentDraft = { fullName: "", email: "", phone: "", password: "" };

/**
 * Registers an agent: a new email creates the agent account, an existing agent account is linked.
 * Example: `<RegisterAgentForm onRegistered={() => void team.reload()} />`
 */
export function RegisterAgentForm({ onRegistered }: { onRegistered: (agent: TeamAgent) => void }) {
  const t = useT(companyTeamMessages);
  const toast = useToast();
  const [draft, setDraft] = useState<AgentDraft>(EMPTY_AGENT);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const set = (key: keyof AgentDraft) => (e: React.ChangeEvent<HTMLInputElement>) => setDraft({ ...draft, [key]: e.target.value });

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const agent = await post<TeamAgent>("/api/companies/me/agents", { ...draft, phone: draft.phone || null, password: draft.password || null });
      setDraft(EMPTY_AGENT);
      toast.notify({ tone: "success", title: t("registeredToast", { name: agent.fullName }) });
      onRegistered(agent);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title={t("registerTitle")} description={t("registerHint")}>
      <ErrorBanner message={error} />
      <form onSubmit={submit} className="space-y-4" data-testid="register-agent">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label={t("fullName")}><Input required name="fullName" value={draft.fullName} onChange={set("fullName")} /></Field>
          <Field label={t("email")}><Input required type="email" name="email" value={draft.email} onChange={set("email")} /></Field>
          <Field label={t("phone")}><Input type="tel" name="phone" value={draft.phone} onChange={set("phone")} /></Field>
          <Field label={t("initialPassword")} hint={t("initialPasswordHint")}>
            <Input type="password" name="password" autoComplete="new-password" value={draft.password} onChange={set("password")} />
          </Field>
        </div>
        <div className="flex justify-end border-t border-line pt-4">
          <Button type="submit" icon="plus" loading={busy}>{t("register")}</Button>
        </div>
      </form>
    </Card>
  );
}
