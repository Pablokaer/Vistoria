"use client";

import { createContext, useCallback, useContext, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { createFormatters, type Formatters } from "./formatting";
import { LOCALE_COOKIE, type Locale } from "./locales";
import { translator, type MessageCatalog, type Translate } from "./translate";

interface I18nState {
  locale: Locale;
  setLocale: (locale: Locale) => void;
}

const I18nContext = createContext<I18nState | null>(null);

const ONE_YEAR_SECONDS = 60 * 60 * 24 * 365;

function rememberLocale(locale: Locale) {
  document.cookie = `${LOCALE_COOKIE}=${locale}; path=/; max-age=${ONE_YEAR_SECONDS}; samesite=lax`;
  // lib/api.ts reads <html lang> for the Accept-Language header, so API messages follow the switch immediately.
  document.documentElement.lang = locale;
}

/**
 * Holds the UI language. The root layout passes the server-resolved locale so the first render already matches.
 * Example: `<I18nProvider initialLocale={await getServerLocale()}>{children}</I18nProvider>`.
 */
export function I18nProvider({ initialLocale, children }: { initialLocale: Locale; children: React.ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(initialLocale);
  const router = useRouter();

  const setLocale = useCallback((next: Locale) => {
    rememberLocale(next);
    setLocaleState(next);
    // Server components (landing, pricing) render text on the server: re-render them in the new language.
    router.refresh();
  }, [router]);

  const value = useMemo(() => ({ locale, setLocale }), [locale, setLocale]);
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

/** Current language and the setter used by the language switcher. */
export function useLocale(): I18nState {
  const state = useContext(I18nContext);
  if (!state) throw new Error("useLocale() was called outside <I18nProvider>; wrap the tree in the root layout's provider.");
  return state;
}

/** Translator for a client component. Example: `const t = useT(shellMessages); t("signOut")`. */
export function useT<K extends string>(catalog: MessageCatalog<K>): Translate<K> {
  const { locale } = useLocale();
  return useMemo(() => translator(catalog, locale), [catalog, locale]);
}

/** Dates, money and enum labels in the current language. Example: `const { formatDate } = useFormatters();`. */
export function useFormatters(): Formatters {
  const { locale } = useLocale();
  return useMemo(() => createFormatters(locale), [locale]);
}
