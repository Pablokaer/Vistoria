// Locale-aware display helpers. Pure functions of the locale so they work the same in server and client code.

import type { Locale } from "./locales";
import { enumMessages } from "./messages/enums";

// en-IE keeps the day-month order and price style the product used before localization; pt-BR uses Brazilian conventions.
export const INTL_TAG: Record<Locale, string> = { en: "en-IE", "pt-BR": "pt-BR" };

export interface Formatters {
  /** "30 Sept 2026" / "30 de set. de 2026"; "—" for empty values. */
  formatDate: (value?: string | null) => string;
  /** Date plus hours and minutes. */
  formatDateTime: (value?: string | null) => string;
  /** Label for an API enum value, e.g. "MoveIn" → "Move In" / "Entrada". */
  humanize: (value?: string | null) => string;
  /** Money from minor units, e.g. (4900, "EUR") → "€49.00" / "€ 49,00". */
  formatMoney: (cents: number, currency: string) => string;
  formatNumber: (value: number) => string;
}

const splitCamelCase = (value: string) => value.replace(/([a-z])([A-Z])/g, "$1 $2");

/** Example: `createFormatters("pt-BR").humanize("MoveOut") // "Saída"`. */
export function createFormatters(locale: Locale): Formatters {
  const tag = INTL_TAG[locale];
  const date = new Intl.DateTimeFormat(tag, { day: "numeric", month: "short", year: "numeric" });
  const dateTime = new Intl.DateTimeFormat(tag, { day: "numeric", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
  const number = new Intl.NumberFormat(tag);
  const labels: Record<string, string> = enumMessages[locale];
  return {
    formatDate: (value) => (value ? date.format(new Date(value)) : "—"),
    formatDateTime: (value) => (value ? dateTime.format(new Date(value)) : "—"),
    humanize: (value) => (value ? labels[value] ?? splitCamelCase(value) : "—"),
    formatMoney: (cents, currency) => new Intl.NumberFormat(tag, { style: "currency", currency }).format(cents / 100),
    formatNumber: (value) => number.format(value),
  };
}
