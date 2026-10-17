// Room names are stored as typed, so rooms created in one language (e.g. the seeded "Living Room", "Bedroom 1")
// would stay in that language. A name that is just a room type label, optionally numbered, is shown in the
// current UI language instead; any other name (e.g. "Master suite") is the company's own text and stays as typed.

import type { Locale } from "./locales";
import { LOCALES } from "./locales";
import { enumMessages } from "./messages/enums";
import { ROOM_TYPES, type RoomType } from "@/lib/types";

// "Other" is left out: "Other" / "Outra" is ambiguous as a room name and reads oddly translated.
const TRANSLATABLE_TYPES = ROOM_TYPES.filter((type) => type !== "Other");

const normalize = (label: string) => label.trim().toLocaleLowerCase();

/** Every known label (any language, lower-cased) → its room type. */
const TYPE_BY_LABEL = new Map<string, RoomType>(
  LOCALES.flatMap((locale) => TRANSLATABLE_TYPES.map((type) => [normalize(enumMessages[locale][type]), type] as const)),
);

const NAME_PATTERN = /^(.*?)(?:\s+(\d+))?$/;

/**
 * Shows a default room name in `locale`; custom names come back unchanged.
 * Example: `localizeRoomName("Bedroom 2", "pt-BR") // "Quarto 2"`, `localizeRoomName("Suíte master", "en") // "Suíte master"`.
 */
export function localizeRoomName(name: string, locale: Locale): string {
  const [, label = "", number] = NAME_PATTERN.exec(name.trim()) ?? [];
  const type = TYPE_BY_LABEL.get(normalize(label));
  // Already in this language: keep the original casing ("Living Room", not "Living room").
  if (!type || normalize(label) === normalize(enumMessages[locale][type])) return name;
  const translated = enumMessages[locale][type].replace(/^./, (c) => c.toLocaleUpperCase());
  return number ? `${translated} ${number}` : translated;
}
