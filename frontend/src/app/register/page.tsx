"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Button, ErrorBanner, Field, Input } from "@/components/ui";
import { ApiError, errorMessage } from "@/lib/api";
import { useT } from "@/i18n/I18nProvider";
import { authMessages } from "@/i18n/messages/auth";
import { homeFor, needsSubscription, safeNext, useAuth } from "@/lib/auth";
import type { Me } from "@/lib/types";

/** Accounts that need a subscription continue to the plan step; others go where they were heading. */
function afterRegistration(user: Me, next: string | null, plan: string | null): string {
  if (needsSubscription(user)) return `/checkout${plan ? `?plan=${encodeURIComponent(plan)}` : ""}`;
  return safeNext(next) ?? homeFor(user);
}

/**
 * Only companies and invited tenants sign up here; inspectors' accounts are created by their company (ADR 0022).
 * `/register?plan=<code>` (the landing page's "Get started") is the company sign-up step of the subscription flow,
 * `/register?invite=<token>` creates a tenant account from the tenancy invitation (ADR 0023), plain `/register` is
 * the company sign-up without a preselected plan.
 */
function RegisterForm() {
  const { register } = useAuth();
  const t = useT(authMessages);
  const router = useRouter();
  const params = useSearchParams();
  const plan = params.get("plan");
  const invitationToken = params.get("invite");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setFieldErrors({});
    try {
      const role = invitationToken ? "Tenant" : "Company";
      const user = await register({ email, password, fullName, role, invitationToken: invitationToken ?? undefined });
      router.replace(afterRegistration(user, params.get("next"), plan));
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.errors).length > 0) setFieldErrors(err.errors);
      else setError(errorMessage(err));
      setBusy(false);
    }
  }

  const form = (
    <form onSubmit={submit} className="space-y-4">
      <ErrorBanner message={error} />
      <Field label={t("fullName")} error={fieldErrors.FullName?.[0]}><Input required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" /></Field>
      <Field label={t("email")} hint={invitationToken ? t("invitedEmailHint") : undefined} error={fieldErrors.Email?.[0]}>
        <Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" />
      </Field>
      <Field label={t("password")} hint={t("passwordHint")} error={fieldErrors.Password?.join(" ")}>
        <Input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
      </Field>
      <Button type="submit" size="lg" className="w-full" loading={busy}>{plan ? t("createAccountContinue") : t("createAccount")}</Button>
      {!plan && !invitationToken && <SignUpHints />}
    </form>
  );
  const signInHref = invitationToken ? `/login?next=${encodeURIComponent(`/tenant/invite/${invitationToken}`)}` : "/login";
  const signIn = <p className="mt-5 border-t border-line pt-4 text-center text-label text-ink-3">{t("alreadyRegistered")} <Link className="font-medium text-brand hover:underline" href={signInHref}>{t("signInLink")}</Link></p>;

  if (invitationToken) return <AuthCard title={t("tenantRegisterTitle")} subtitle={t("tenantRegisterSubtitle")}>{form}{signIn}</AuthCard>;
  if (!plan) return <AuthCard title={t("registerTitle")} subtitle={t("registerSubtitle")}>{form}{signIn}</AuthCard>;
  return (
    <CheckoutLayout step="account" title={t("companyRegisterTitle")} subtitle={t("companyRegisterSubtitle")}>
      <div className="rounded-lg border border-line bg-surface p-5 shadow-card sm:p-6">{form}{signIn}</div>
    </CheckoutLayout>
  );
}

/** Where tenants and inspectors get their accounts instead. */
function SignUpHints() {
  const t = useT(authMessages);
  return (
    <div className="space-y-1 text-caption text-ink-3">
      <p>{t("tenantAccountHint")}</p>
      <p>{t("inspectorAccountHint")}</p>
    </div>
  );
}

export default function RegisterPage() {
  return <Suspense><RegisterForm /></Suspense>;
}
