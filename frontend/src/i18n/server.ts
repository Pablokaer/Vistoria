// Locale for server components (landing, pricing, root layout). Reading the cookie/header makes those routes
// dynamic, which is the price of serving the right language on the very first byte (no flash of English).

import { cookies, headers } from "next/headers";
import { LOCALE_COOKIE, resolveLocale, type Locale } from "./locales";
import { translator, type MessageCatalog, type Translate } from "./translate";

/** The request's language: the `locale` cookie if set, otherwise the browser's Accept-Language. */
export async function getServerLocale(): Promise<Locale> {
  const [cookieStore, headerList] = await Promise.all([cookies(), headers()]);
  return resolveLocale(cookieStore.get(LOCALE_COOKIE)?.value, headerList.get("accept-language"));
}

/**
 * Translator for a server component. Example: `const t = await getServerTranslator(landingMessages); t("heroTitle")`.
 */
export async function getServerTranslator<K extends string>(catalog: MessageCatalog<K>): Promise<Translate<K>> {
  return translator(catalog, await getServerLocale());
}
