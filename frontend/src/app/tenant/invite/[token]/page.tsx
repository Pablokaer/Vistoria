"use client";

import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, LinkButton, Loading } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";
import { errorMessage, post } from "@/lib/api";
import { useApi } from "@/lib/hooks";

interface Preview { propertyAddress: string; companyName: string; invitedEmail: string; expiresAt: string }

/** Rendered outside the tenant layout: the visitor may not have an account yet. */
export default function TenantInvitePage() {
  const { token } = useParams<{ token: string }>();
  const router = useRouter();
  const t = useT(tenantMessages);
  const { data, error, loading } = useApi<Preview>(`/api/tenant/invitations/${encodeURIComponent(token)}`);
  const [busy, setBusy] = useState(false);
  const [acceptError, setAcceptError] = useState<string | null>(null);

  async function accept() {
    setBusy(true);
    setAcceptError(null);
    try { await post(`/api/tenant/invitations/${encodeURIComponent(token)}/accept`); router.replace("/tenant"); }
    catch (e) { setAcceptError(errorMessage(e)); setBusy(false); }
  }

  if (loading) return <Loading />;
  if (error || !data) return <AuthCard title={t("inviteUnavailable")}><ErrorBanner message={t("inviteInvalid")} /></AuthCard>;
  const next = encodeURIComponent(`/tenant/invite/${token}`);
  return (
    <AuthCard title={t("joinTenancy")} subtitle={t("inviteSubtitle", { company: data.companyName, email: data.invitedEmail, address: data.propertyAddress })}>
      <ErrorBanner message={acceptError} />
      <div className="space-y-3">
        <Button size="lg" className="w-full" loading={busy} onClick={() => void accept()}>{t("acceptInvitation")}</Button>
        <p className="text-center text-sm text-ink-3">{t("notSignedIn")}</p>
        <div className="grid grid-cols-2 gap-2">
          <LinkButton variant="secondary" href={`/login?next=${next}`}>{t("signIn")}</LinkButton>
          <LinkButton variant="secondary" href={`/register?role=Tenant&next=${next}`}>{t("createAccount")}</LinkButton>
        </div>
      </div>
    </AuthCard>
  );
}
