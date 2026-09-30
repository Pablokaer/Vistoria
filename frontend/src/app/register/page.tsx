"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { CheckoutLayout } from "@/components/billing/CheckoutLayout";
import { Button, ErrorBanner, Field, Input } from "@/components/ui";
import { ApiError, errorMessage } from "@/lib/api";
import { homeFor, needsSubscription, safeNext, useAuth } from "@/lib/auth";
import type { Me, Role } from "@/lib/types";

const ROLES: { value: Role; title: string; text: string }[] = [
  { value: "Company", title: "Company", text: "I manage properties and order inspections." },
  { value: "Agent", title: "Inspector", text: "I carry out inspections." },
  { value: "Tenant", title: "Tenant", text: "I want to review my inspection report." },
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
      <Field label="Full name" error={fieldErrors.FullName?.[0]}><Input required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" /></Field>
      <Field label="Email" error={fieldErrors.Email?.[0]}><Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" /></Field>
      <Field label="Password" hint="At least 8 characters with upper-case, lower-case and a digit." error={fieldErrors.Password?.join(" ")}>
        <Input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
      </Field>
      <Button type="submit" size="lg" className="w-full" loading={busy}>{plan ? "Create account & continue" : "Create account"}</Button>
    </form>
  );
  const signIn = <p className="mt-6 text-center text-sm text-slate-600">Already registered? <Link className="font-medium text-brand hover:underline" href="/login">Sign in</Link></p>;

  if (!plan) return <AuthCard title="Create your account" subtitle="Choose how you will use InspectFlow.">{form}{signIn}</AuthCard>;
  return (
    <CheckoutLayout step="account" title="Create your company account" subtitle="Next you will choose your plan and pay securely. Inspectors and tenants join for free.">
      <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">{form}{signIn}</div>
    </CheckoutLayout>
  );
}

function RolePicker({ role, onChange }: { role: Role; onChange: (role: Role) => void }) {
  return (
    <fieldset className="grid gap-2">
      {ROLES.map((r) => (
        <label key={r.value} className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 ${role === r.value ? "border-brand bg-brand-50" : "border-slate-200"}`}>
          <input type="radio" name="role" className="mt-1 accent-brand" checked={role === r.value} onChange={() => onChange(r.value)} />
          <span><span className="block text-sm font-medium">{r.title}</span><span className="block text-xs text-slate-600">{r.text}</span></span>
        </label>
      ))}
    </fieldset>
  );
}

export default function RegisterPage() {
  return <Suspense><RegisterForm /></Suspense>;
}
