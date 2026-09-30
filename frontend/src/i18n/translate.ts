// Framework-free translation core, usable from server components, client components and plain modules.
// Catalogs are plain objects per area (src/i18n/messages/*.ts); the Portuguese side is type-checked to have
// exactly the English keys, so a missing translation is a compile error, not a runtime blank.

import type { Locale } from "./locales";

/** Values interpolated into `{name}` placeholders. */
export type MessageValues = Record<string, string | number>;

export type MessageCatalog<K extends string> = Record<Locale, Record<K, string>>;

export type Translate<K extends string> = (key: K, values?: MessageValues) => string;

/**
 * Declares one area's texts. English is the source of truth for the keys; pt-BR must provide every one.
 * Example: `export const shellMessages = defineMessages({ signOut: "Sign out" }, { signOut: "Sair" });`
 */
export function defineMessages<K extends string>(en: Record<K, string>, ptBR: Record<NoInfer<K>, string>): MessageCatalog<K> {
  return { en, "pt-BR": ptBR };
}

/**
 * Replaces `{name}` placeholders; unknown placeholders are left visible so a missing value is noticed.
 * Example: `interpolate("Hello {name}", { name: "Ana" }) // "Hello Ana"`.
 */
export function interpolate(template: string, values?: MessageValues): string {
  if (!values) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) => (name in values ? String(values[name]) : match));
}

/**
 * Binds a catalog to a locale. Example: `const t = translator(shellMessages, "pt-BR"); t("signOut") // "Sair"`.
 */
export function translator<K extends string>(catalog: MessageCatalog<K>, locale: Locale): Translate<K> {
  const texts = catalog[locale];
  return (key, values) => interpolate(texts[key], values);
}
