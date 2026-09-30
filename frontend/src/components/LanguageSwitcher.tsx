"use client";

import { useLocale, useT } from "@/i18n/I18nProvider";
import { isLocale, LOCALE_NAMES, LOCALE_SHORT_NAMES, LOCALES } from "@/i18n/locales";
import { commonMessages } from "@/i18n/messages/common";

/** Compact EN / PT select; the choice is remembered in a cookie and applies to the whole site. */
export function LanguageSwitcher({ className }: { className?: string }) {
  const { locale, setLocale } = useLocale();
  const t = useT(commonMessages);
  return (
    <label className={`inline-flex items-center ${className ?? ""}`}>
      <span className="sr-only">{t("language")}</span>
      <select value={locale} onChange={(e) => { if (isLocale(e.target.value)) setLocale(e.target.value); }}
        title={LOCALE_NAMES[locale]} data-testid="language-switcher"
        className="rounded-md border border-slate-300 bg-white px-2 py-1 text-sm text-slate-700 hover:border-slate-400">
        {LOCALES.map((l) => <option key={l} value={l} title={LOCALE_NAMES[l]}>{LOCALE_SHORT_NAMES[l]}</option>)}
      </select>
    </label>
  );
}
