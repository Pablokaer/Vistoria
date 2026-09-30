"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AcceptPanel } from "@/components/AcceptPanel";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, Field, Input, LinkButton, Loading } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { errorMessage, post } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { InvitationPreview } from "@/lib/types";

export default function PrivateInvitePage() {
  const { token } = useParams<{ token: string }>();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();
  const t = useT(agentMessages);
  const isAgent = !!user?.roles.includes("Agent");
  const { data, setData, error, loading } = useApi<InvitationPreview>(isAgent ? `/api/agent/invitations/${encodeURIComponent(token)}` : null);
  const [code, setCode] = useState("");
  const [verifyError, setVerifyError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!authLoading && !user) router.replace(`/login?next=${encodeURIComponent(`/inspection/invite/${token}`)}`);
  }, [authLoading, user, router, token]);

  async function verify(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setVerifyError(null);
    try { setData(await post<InvitationPreview>(`/api/agent/invitations/${encodeURIComponent(token)}/verify`, { accessCode: code })); }
    catch (err) { setVerifyError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  if (authLoading || !user) return <Loading />;
  if (!isAgent) return <AuthCard title={t("inspectorRequiredTitle")}><p className="text-sm text-slate-600">{t("inspectorRequiredBody")}</p></AuthCard>;

  if (data && !data.requiresCode) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-8">
        <h1 className="mb-4 text-2xl font-semibold">{t("privateInspection")}</h1>
        {data.inspection ? <AcceptPanel item={data.inspection} /> : (
          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <p className="text-sm text-slate-700">{t("noLongerOpen")}</p>
            <LinkButton className="mt-4" href="/agent/inspections">{t("myInspectionsTitle")}</LinkButton>
          </div>
        )}
      </div>
    );
  }

  return (
    <AuthCard title={t("privateInviteTitle")} subtitle={t("privateInviteSubtitle")}>
      <ErrorBanner message={error ? t("inviteInvalid") : verifyError} />
      {loading ? <Loading /> : data && (
        <form onSubmit={verify} className="space-y-4">
          <Field label={t("accessCode")} hint={t("attemptsRemaining", { count: data.attemptsRemaining })}>
            <Input inputMode="numeric" pattern="[0-9]{6}" maxLength={6} autoComplete="one-time-code" required value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))} className="text-center font-mono text-2xl tracking-[0.5em]" />
          </Field>
          <Button type="submit" size="lg" className="w-full" loading={busy} disabled={code.length !== 6}>{t("verifyCode")}</Button>
        </form>
      )}
    </AuthCard>
  );
}
