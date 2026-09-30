"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icons";
import { Button, Card, ErrorBanner, Field, Input, PageHeader } from "@/components/ui";
import { useLocale, useT } from "@/i18n/I18nProvider";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { errorMessage, post } from "@/lib/api";
import { useAuth } from "@/lib/auth";

export default function OnboardingPage() {
  const { user, reload } = useAuth();
  const { locale } = useLocale();
  const t = useT(companyPageMessages);
  const router = useRouter();
  const [name, setName] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => { if (user?.company) router.replace("/company"); }, [user, router]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      // The owner's UI language becomes the default report language; it can be changed later in Settings.
      await post("/api/companies", { name, contactEmail: contactEmail || null, phone: phone || null, reportLanguage: locale });
      await reload();
      router.replace("/company");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-narrow">
      <PageHeader eyebrow={t("workspaceStep")} title={t("onboardingTitle")} subtitle={t("onboardingSubtitle")} />
      <Card>
        <ErrorBanner message={error} />
        <form onSubmit={submit} className="space-y-5">
          <Field label={t("companyName")}><Input required value={name} onChange={(e) => setName(e.target.value)} placeholder={t("companyNamePlaceholder")} /></Field>
          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
            <Field label={t("contactEmail")} hint={t("contactEmailHint")}><Input type="email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} /></Field>
            <Field label={t("phone")}><Input value={phone} onChange={(e) => setPhone(e.target.value)} /></Field>
          </div>
          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line pt-4">
            <p className="flex max-w-sm items-start gap-2 text-caption text-ink-3"><Icon name="info" className="mt-0.5 h-4 w-4 shrink-0" />{t("onboardingAside")}</p>
            <Button type="submit" size="lg" loading={busy}>{t("createWorkspace")}</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
