"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { AcceptPanel } from "@/components/AcceptPanel";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, Field, Input, LinkButton, Loading } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { InvitationPreview } from "@/lib/types";

export default function PrivateInvitePage() {
  const { token } = useParams<{ token: string }>();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();
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
  if (!isAgent) return <AuthCard title="Inspector account required"><p className="text-sm text-slate-600">This invitation is for an inspector. Sign in with an inspector account.</p></AuthCard>;

  if (data && !data.requiresCode) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-8">
        <h1 className="mb-4 text-2xl font-semibold">Private inspection</h1>
        {data.inspection ? <AcceptPanel item={data.inspection} /> : (
          <div className="rounded-xl border border-slate-200 bg-white p-5">
            <p className="text-sm text-slate-700">This inspection is no longer open. If you already accepted it, it is in your inspections.</p>
            <LinkButton className="mt-4" href="/agent/inspections">My inspections</LinkButton>
          </div>
        )}
      </div>
    );
  }

  return (
    <AuthCard title="Private inspection invitation" subtitle="Enter the 6-digit access code the company gave you.">
      <ErrorBanner message={error ? "This invitation link is invalid, expired or has already been used." : verifyError} />
      {loading ? <Loading /> : data && (
        <form onSubmit={verify} className="space-y-4">
          <Field label="Access code" hint={`${data.attemptsRemaining} attempt(s) remaining`}>
            <Input inputMode="numeric" pattern="[0-9]{6}" maxLength={6} autoComplete="one-time-code" required value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))} className="text-center font-mono text-2xl tracking-[0.5em]" />
          </Field>
          <Button type="submit" size="lg" className="w-full" loading={busy} disabled={code.length !== 6}>Verify code</Button>
        </form>
      )}
    </AuthCard>
  );
}
