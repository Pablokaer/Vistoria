"use client";

import { Icon } from "@/components/icons";
import { useLocale, useT } from "@/i18n/I18nProvider";
import { isLocale, LOCALE_NAMES, LOCALE_SHORT_NAMES, LOCALES } from "@/i18n/locales";
import { commonMessages } from "@/i18n/messages/common";
import { cx } from "@/lib/cx";

/**
 * Compact EN / PT select; the choice is remembered in a cookie and applies to the whole site.
 * `tone="dark"` is for the navy sidebar.
 */
export function LanguageSwitcher({ className, tone = "light" }: { className?: string; tone?: "light" | "dark" }) {
  const { locale, setLocale } = useLocale();
  const t = useT(commonMessages);
  return (
    <label className={cx("relative inline-flex items-center", className)}>
      <span className="sr-only">{t("language")}</span>
      <Icon name="globe" className={cx("pointer-events-none absolute left-2 h-4 w-4", tone === "dark" ? "text-sidebar-ink" : "text-ink-3")} />
      <select value={locale} onChange={(e) => { if (isLocale(e.target.value)) setLocale(e.target.value); }}
        title={LOCALE_NAMES[locale]} data-testid="language-switcher"
        className={cx("appearance-none rounded-md py-1 pl-7 pr-2 text-label font-medium transition",
          tone === "dark" ? "bg-transparent text-sidebar-ink hover:bg-white/5 [&>option]:text-ink" : "border border-line-strong bg-surface text-ink-2 hover:border-ink-4")}>
        {LOCALES.map((l) => <option key={l} value={l} title={LOCALE_NAMES[l]}>{LOCALE_SHORT_NAMES[l]}</option>)}
      </select>
    </label>
  );
}
