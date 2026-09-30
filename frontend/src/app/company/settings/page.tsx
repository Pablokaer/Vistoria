"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { Button, Card, ErrorBanner, Field, Notice, PageHeader, Select, useToast } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { isLocale, LOCALE_NAMES, LOCALES, type Locale } from "@/i18n/locales";
import { companyPageMessages } from "@/i18n/messages/company-pages";
import { errorMessage, put } from "@/lib/api";
import { useAuth } from "@/lib/auth";

// Owner and Admin may change company settings; the API enforces it (403) — this only avoids a pointless click.
const SETTINGS_ROLES = ["Owner", "Admin"];

/** Company-wide settings. Today: the language of future reports and AI descriptions. */
export default function CompanySettingsPage() {
  const { user } = useAuth();
  const t = useT(companyPageMessages);
  const company = user?.company;
  if (!company) return null;
  return (
    <div className="mx-auto max-w-narrow">
      <PageHeader title={t("settingsTitle")} subtitle={t("settingsSubtitle", { company: company.companyName })} />
      <section className="grid grid-cols-1 gap-4 md:grid-cols-[12rem_minmax(0,1fr)]">
        <div>
          <h2 className="flex items-center gap-2 text-section font-semibold text-ink"><Icon name="file" className="h-5 w-5 text-ink-3" />{t("settingsGeneral")}</h2>
        </div>
        <ReportLanguageCard current={company.reportLanguage} canEdit={SETTINGS_ROLES.includes(company.role)} />
      </section>
    </div>
  );
}

function ReportLanguageCard({ current, canEdit }: { current: Locale; canEdit: boolean }) {
  const { reload } = useAuth();
  const t = useT(companyPageMessages);
  const toast = useToast();
  const [language, setLanguage] = useState<Locale>(current);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await put("/api/companies/me/report-language", { language });
      await reload();
      toast.notify({ tone: "success", title: t("reportLanguageSaved") });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title={t("reportLanguage")} description={t("reportLanguageCurrent", { language: LOCALE_NAMES[current] })}>
      <ErrorBanner message={error} />
      {!canEdit && <Notice>{t("ownerAdminOnly")}</Notice>}
      <form onSubmit={save} className="space-y-4">
        <Field label={t("reportLanguage")} hint={t("reportLanguageHint")}>
          <Select value={language} disabled={!canEdit} data-testid="report-language"
            onChange={(e) => { if (isLocale(e.target.value)) setLanguage(e.target.value); }}>
            {LOCALES.map((l) => <option key={l} value={l}>{LOCALE_NAMES[l]}</option>)}
          </Select>
        </Field>
        <div className="flex justify-end border-t border-line pt-4">
          <Button type="submit" loading={busy} disabled={!canEdit || language === current}>{t("saveReportLanguage")}</Button>
        </div>
      </form>
    </Card>
  );
}
