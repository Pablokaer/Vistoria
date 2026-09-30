"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, Field, Input } from "@/components/ui";
import { ApiError, errorMessage } from "@/lib/api";
import { homeFor, useAuth } from "@/lib/auth";
import type { Role } from "@/lib/types";

const ROLES: { value: Role; title: string; text: string }[] = [
  { value: "Company", title: "Company", text: "I manage properties and order inspections." },
  { value: "Agent", title: "Inspector", text: "I carry out inspections." },
  { value: "Tenant", title: "Tenant", text: "I want to review my inspection report." },
];

function RegisterForm() {
  const { register } = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const initialRole = (params.get("role") as Role) ?? "Company";
  const [role, setRole] = useState<Role>(ROLES.some((r) => r.value === initialRole) ? initialRole : "Company");
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
      const next = params.get("next");
      router.replace(next && next.startsWith("/") && !next.startsWith("//") ? next : homeFor(user));
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.errors).length > 0) setFieldErrors(err.errors);
      else setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Create your account" subtitle="Choose how you will use InspectFlow.">
      <ErrorBanner message={error} />
      <form onSubmit={submit} className="space-y-4">
        <fieldset className="grid gap-2">
          {ROLES.map((r) => (
            <label key={r.value} className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 ${role === r.value ? "border-brand bg-brand-50" : "border-slate-200"}`}>
              <input type="radio" name="role" className="mt-1 accent-[#0f4c5c]" checked={role === r.value} onChange={() => setRole(r.value)} />
              <span><span className="block text-sm font-medium">{r.title}</span><span className="block text-xs text-slate-600">{r.text}</span></span>
            </label>
          ))}
        </fieldset>
        <Field label="Full name" error={fieldErrors.FullName?.[0]}><Input required value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="name" /></Field>
        <Field label="Email" error={fieldErrors.Email?.[0]}><Input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" /></Field>
        <Field label="Password" hint="At least 8 characters with upper-case, lower-case and a digit." error={fieldErrors.Password?.join(" ")}>
          <Input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" />
        </Field>
        <Button type="submit" size="lg" className="w-full" loading={busy}>Create account</Button>
      </form>
      <p className="mt-6 text-center text-sm text-slate-600">Already registered? <Link className="font-medium text-brand hover:underline" href="/login">Sign in</Link></p>
    </AuthCard>
  );
}

export default function RegisterPage() {
  return <Suspense><RegisterForm /></Suspense>;
}
