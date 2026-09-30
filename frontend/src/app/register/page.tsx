"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Icon, type IconName } from "@/components/icons";
import { Button, ErrorBanner, Field, Input } from "@/components/ui";
import { ApiError, errorMessage } from "@/lib/api";
import { useT } from "@/i18n/I18nProvider";
import { authMessages, type AuthKey } from "@/i18n/messages/auth";
import { homeFor, needsSubscription, safeNext, useAuth } from "@/lib/auth";
import type { Me, Role } from "@/lib/types";

const ROLES: { value: Role; title: AuthKey; text: AuthKey; icon: IconName }[] = [
  { value: "Company", title: "roleCompanyTitle", text: "roleCompanyText", icon: "building" },
  { value: "Agent", title: "roleAgentTitle", text: "roleAgentText", icon: "clipboard" },
  { value: "Tenant", title: "roleTenantTitle", text: "roleTenantText", icon: "user" },
];

/** Accounts that need a subscription continue to the plan step; others go where they were heading. */
function afterRegistration(user: Me, next: string | null, plan: string | null): string {
  if (needsSubscription(user)) return `/checkout${plan ? `?plan=${encodeURIComponent(plan)}` : ""}`;
  return safeNext(next) ?? homeFor(user);
}

/**
 * `/register?plan=<code>` (the landing page's "Get started") is the company sign-up step of the subscription flow;
 * plain `/register` keeps the role choice for inspectors and tenants (e.g. from invitation links).
 */
function RegisterForm() {
  const { register } = useAuth();
  const t = useT(authMessages);
  const router = useRouter();
  const params = useSearchParams();
  const plan = params.get("plan");
  const initialRole = (params.get("role") as Role) ?? "Company";
  const [role, setRole] = useState<Role>(plan ? "Company" : ROLES.some((r) => r.value === initialRole) ? initialRole : "Company");
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
      const user = await register({ email, password, fullName, role });
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
      {!plan && <RolePicker role={role} onChange={setRole} />}
      <Field label={t("fullName")} error={fieldErrors.FullName?.[0]}><Input required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" /></Field>
      <Field label={t("email")} error={fieldErrors.Email?.[0]}><Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" /></Field>
      <Field label={t("password")} hint={t("passwordHint")} error={fieldErrors.Password?.join(" ")}>
        <Input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
      </Field>
      <Button type="submit" size="lg" className="w-full" loading={busy}>{plan ? t("createAccountContinue") : t("createAccount")}</Button>
    </form>
  );
  const signIn = <p className="mt-5 border-t border-line pt-4 text-center text-label text-ink-3">{t("alreadyRegistered")} <Link className="font-medium text-brand hover:underline" href="/login">{t("signInLink")}</Link></p>;

  if (!plan) return <AuthCard title={t("registerTitle")} subtitle={t("registerSubtitle")}>{form}{signIn}</AuthCard>;
  return (
    <CheckoutLayout step="account" title={t("companyRegisterTitle")} subtitle={t("companyRegisterSubtitle")}>
      <div className="rounded-lg border border-line bg-surface p-5 shadow-card sm:p-6">{form}{signIn}</div>
    </CheckoutLayout>
  );
}

function RolePicker({ role, onChange }: { role: Role; onChange: (role: Role) => void }) {
  const t = useT(authMessages);
  return (
    <fieldset className="grid gap-2">
      <legend className="mb-1.5 text-label font-medium text-ink-2">{t("roleLegend")}</legend>
      {ROLES.map((r) => (
        <label key={r.value} className={`flex cursor-pointer items-start gap-3 rounded-md border p-3 transition ${role === r.value ? "border-brand bg-brand-50 ring-1 ring-brand/20" : "border-line hover:border-line-strong"}`}>
          <input type="radio" name="role" className="mt-1 accent-brand" checked={role === r.value} onChange={() => onChange(r.value)} />
          <Icon name={r.icon} className={`mt-0.5 h-5 w-5 shrink-0 ${role === r.value ? "text-brand" : "text-ink-4"}`} />
          <span><span className="block text-body font-medium text-ink">{t(r.title)}</span><span className="block text-caption text-ink-3">{t(r.text)}</span></span>
        </label>
      ))}
    </fieldset>
  );
}

export default function RegisterPage() {
  return <Suspense><RegisterForm /></Suspense>;
}
