"use client";

import { Icon } from "@/components/icons";
import { useLocale, useT } from "@/i18n/I18nProvider";
import { LOCALE_NAMES, LOCALE_SHORT_NAMES, type Locale } from "@/i18n/locales";
import { commonMessages } from "@/i18n/messages/common";
import { cx } from "@/lib/cx";

// Portuguese first: most users of the app are Brazilian.
const TOGGLE_ORDER: Locale[] = ["pt-BR", "en"];

/**
 * Two-position PT | EN switch for the navy sidebar; one click changes the whole app (choice kept in the locale cookie).
 * Example: `<LanguageToggle />`
 */
export function LanguageToggle() {
  const { locale, setLocale } = useLocale();
  const t = useT(commonMessages);
  return (
    <div className="flex items-center justify-between gap-3 px-1">
      <span className="inline-flex items-center gap-1.5 text-label font-medium text-sidebar-ink">
        <Icon name="globe" className="h-4 w-4" />{t("language")}
      </span>
      <div role="radiogroup" aria-label={t("language")} data-testid="sidebar-language-toggle"
        className="inline-flex rounded-full bg-white/10 p-0.5">
        {TOGGLE_ORDER.map((l) => (
          <button key={l} type="button" role="radio" aria-checked={locale === l} title={LOCALE_NAMES[l]} onClick={() => setLocale(l)}
            className={cx("min-w-10 rounded-full px-3 py-1 text-caption font-semibold transition",
              locale === l ? "bg-white text-sidebar shadow-card" : "text-sidebar-ink hover:text-white")}>
            {LOCALE_SHORT_NAMES[l]}
          </button>
        ))}
      </div>
    </div>
  );
}
