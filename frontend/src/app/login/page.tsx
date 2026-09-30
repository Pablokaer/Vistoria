"use client";

import Link from "next/link";
import { Suspense, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthCard } from "@/components/AuthCard";
import { Button, ErrorBanner, Field, Input, Notice } from "@/components/ui";
import { errorMessage } from "@/lib/api";
import { useT } from "@/i18n/I18nProvider";
import { authMessages } from "@/i18n/messages/auth";
import { destinationAfterSignIn, useAuth } from "@/lib/auth";

function LoginForm() {
  const { login } = useAuth();
  const t = useT(authMessages);
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
    <AuthCard title={t("signInTitle")} subtitle={t("signInSubtitle")}>
      {params.get("expired") && <Notice tone="warning">{t("sessionExpired")}</Notice>}
      <ErrorBanner message={error} />
      <form onSubmit={submit} className="space-y-4">
        <Field label={t("email")}><Input type="email" autoComplete="email" required value={email} onChange={(e) => setEmail(e.target.value)} /></Field>
        <Field label={t("password")}><Input type="password" autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} /></Field>
        <Button type="submit" size="lg" className="w-full" loading={busy}>{t("signInButton")}</Button>
      </form>
      <p className="mt-5 border-t border-line pt-4 text-center text-label text-ink-3">
        {t("noAccount")} <Link className="font-medium text-brand hover:underline" href={`/register${params.get("next") ? `?next=${encodeURIComponent(params.get("next")!)}` : ""}`}>{t("createOne")}</Link>
      </p>
    </AuthCard>
  );
}

export default function LoginPage() {
  return <Suspense><LoginForm /></Suspense>;
}
