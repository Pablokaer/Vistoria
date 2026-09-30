// Supported UI languages. Kept in step with backend SupportedLanguages (en, pt-BR) so the Accept-Language
// header the web app sends always names a language the API can answer in.

export const LOCALES = ["en", "pt-BR"] as const;
export type Locale = (typeof LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "en";

/** The user's explicit choice; without it the browser's Accept-Language decides. */
export const LOCALE_COOKIE = "locale";

/** Names shown in the language switcher, each in its own language. */
export const LOCALE_NAMES: Record<Locale, string> = { en: "English", "pt-BR": "Português (Brasil)" };

/** Short labels for compact switchers. */
export const LOCALE_SHORT_NAMES: Record<Locale, string> = { en: "EN", "pt-BR": "PT" };

/** Type guard for cookie / user input. Example: `isLocale("pt-BR") // true`. */
export function isLocale(value: unknown): value is Locale {
  return typeof value === "string" && (LOCALES as readonly string[]).includes(value);
}

/**
 * Picks the first supported language from an Accept-Language header; any Portuguese variant maps to pt-BR.
 * Example: `localeFromAcceptLanguage("pt-PT,pt;q=0.9,en;q=0.8") // "pt-BR"`.
 */
export function localeFromAcceptLanguage(header: string | null | undefined): Locale {
  const tags = (header ?? "").split(",").map((part) => part.split(";")[0].trim().toLowerCase());
  for (const tag of tags) {
    if (tag.startsWith("pt")) return "pt-BR";
    if (tag.startsWith("en")) return "en";
  }
  return DEFAULT_LOCALE;
}

/** Cookie wins over the browser header, so a user's explicit choice sticks. */
export function resolveLocale(cookieValue: string | null | undefined, acceptLanguage: string | null | undefined): Locale {
  return isLocale(cookieValue) ? cookieValue : localeFromAcceptLanguage(acceptLanguage);
}
