"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, Field, Input, Notice } from "@/components/ui";
import { errorMessage } from "@/lib/api";
import { destinationAfterSignIn, useAuth } from "@/lib/auth";

function LoginForm() {
  const { login } = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const user = await login(email, password);
      router.replace(destinationAfterSignIn(user, params.get("next")));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthCard title="Sign in" subtitle="Companies, inspectors and tenants all sign in here.">
      {params.get("expired") && <Notice tone="warning">Your session expired. Please sign in again.</Notice>}
      <ErrorBanner message={error} />
      <form onSubmit={submit} className="space-y-4">
        <Field label="Email"><Input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
        <Field label="Password"><Input type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} /></Field>
        <Button type="submit" size="lg" className="w-full" loading={busy}>Sign in</Button>
      </form>
      <p className="mt-6 text-center text-sm text-slate-600">
        No account? <Link className="font-medium text-brand hover:underline" href={`/register${params.get("next") ? `?next=${encodeURIComponent(params.get("next")!)}` : ""}`}>Create one</Link>
      </p>
    </AuthCard>
  );
}

export default function LoginPage() {
  return <Suspense><LoginForm /></Suspense>;
}
