"use client";

import { useState } from "react";
import { Button, Card, ErrorBanner, Field, Notice, PageHeader, Select } from "@/components/ui";
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
    <div className="mx-auto max-w-2xl">
      <PageHeader title={t("settingsTitle")} subtitle={t("settingsSubtitle", { company: company.companyName })} />
      <ReportLanguageCard current={company.reportLanguage} canEdit={SETTINGS_ROLES.includes(company.role)} />
    </div>
  );
}

function ReportLanguageCard({ current, canEdit }: { current: Locale; canEdit: boolean }) {
  const { reload } = useAuth();
  const t = useT(companyPageMessages);
  const [language, setLanguage] = useState<Locale>(current);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setSaved(false);
    try {
      await put("/api/companies/me/report-language", { language });
      await reload();
      setSaved(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card title={t("reportLanguage")}>
      <ErrorBanner message={error} />
      {saved && <Notice tone="success">{t("reportLanguageSaved")}</Notice>}
      {!canEdit && <Notice>{t("ownerAdminOnly")}</Notice>}
      <form onSubmit={save} className="space-y-4">
        <Field label={t("reportLanguage")} hint={t("reportLanguageHint")}>
          <Select value={language} disabled={!canEdit} data-testid="report-language"
            onChange={(e) => { if (isLocale(e.target.value)) { setLanguage(e.target.value); setSaved(false); } }}>
            {LOCALES.map((l) => <option key={l} value={l}>{LOCALE_NAMES[l]}</option>)}
          </Select>
        </Field>
        <Button type="submit" loading={busy} disabled={!canEdit || language === current}>{t("saveReportLanguage")}</Button>
      </form>
    </Card>
  );
}
