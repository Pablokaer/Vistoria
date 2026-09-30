"use client";

import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
import { ROOM_TYPES, type RoomType } from "@/lib/types";
import { IconButton, Input, Select } from "./ui";

export interface RoomDraft { key: string; roomType: RoomType; name: string }

/**
 * The starting rooms of "Add property", named in the current UI language (names are stored as typed).
 * Example: `const [rooms, setRooms] = useState<RoomDraft[]>(useDefaultRooms());`
 */
export function useDefaultRooms(): RoomDraft[] {
  const t = useT(companyFormMessages);
  return [
    { key: "1", roomType: "LivingRoom", name: t("defaultRoomLiving") },
    { key: "2", roomType: "Bedroom", name: t("defaultRoomBedroom1") },
    { key: "3", roomType: "Bedroom", name: t("defaultRoomBedroom2") },
    { key: "4", roomType: "Kitchen", name: t("defaultRoomKitchen") },
    { key: "5", roomType: "Bathroom", name: t("defaultRoomBathroom") },
  ];
}

const defaultName = (label: string, type: RoomType, rooms: RoomDraft[]) => {
  const base = label.replace(/^./, (c) => c.toUpperCase());
  const same = rooms.filter((r) => r.roomType === type).length;
  return type === "Bedroom" || same > 0 ? `${base} ${same + 1}` : base;
};

const QUICK_ADD_TYPES: RoomType[] = ["Bedroom", "Bathroom", "Kitchen", "LivingRoom", "Garage", "Garden", "Other"];

/** Editable list of rooms used when creating a property. */
export function RoomListEditor({ rooms, onChange }: { rooms: RoomDraft[]; onChange: (rooms: RoomDraft[]) => void }) {
  const t = useT(companyFormMessages);
  const { humanize } = useFormatters();
  const update = (key: string, patch: Partial<RoomDraft>) => onChange(rooms.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  const move = (index: number, delta: number) => {
    const next = [...rooms];
    const [item] = next.splice(index, 1);
    next.splice(index + delta, 0, item);
    onChange(next);
  };
  const add = (type: RoomType) => onChange([...rooms, { key: crypto.randomUUID(), roomType: type, name: defaultName(humanize(type), type, rooms) }]);
  return (
    <div>
      <ol className="divide-y divide-line overflow-hidden rounded-md border border-line">
        {rooms.map((room, i) => (
          <li key={room.key} className="flex flex-wrap items-center gap-2 bg-surface px-2 py-2 transition hover:bg-surface-2 sm:flex-nowrap">
            <span className="tabular w-7 shrink-0 text-center text-caption font-semibold text-ink-3">{i + 1}</span>
            <Select className="min-w-0 flex-1 sm:w-44 sm:flex-none" aria-label={t("roomType")} value={room.roomType} onChange={(e) => update(room.key, { roomType: e.target.value as RoomType })}>
              {ROOM_TYPES.map((type) => <option key={type} value={type}>{humanize(type)}</option>)}
            </Select>
            <div className="flex min-w-0 basis-full items-center gap-1 pl-9 sm:basis-auto sm:flex-1 sm:pl-0">
              <Input className="min-w-0 flex-1" value={room.name} onChange={(e) => update(room.key, { name: e.target.value })} aria-label={t("roomName")} required />
              <IconButton icon="chevronDown" className="rotate-180" disabled={i === 0} onClick={() => move(i, -1)} label={t("moveUp")} />
              <IconButton icon="chevronDown" disabled={i === rooms.length - 1} onClick={() => move(i, 1)} label={t("moveDown")} />
              <IconButton icon="trash" className="hover:text-danger-700" onClick={() => onChange(rooms.filter((r) => r.key !== room.key))} label={t("removeRoom")} />
            </div>
          </li>
        ))}
      </ol>
      <div className="mt-3 flex flex-wrap items-center gap-1.5">
        <span className="mr-1 text-caption font-medium text-ink-3">{t("quickAdd")}</span>
        {QUICK_ADD_TYPES.map((type) => (
          <button key={type} type="button" onClick={() => add(type)}
            className="rounded-sm border border-dashed border-line-strong px-2 py-1 text-label font-medium text-ink-2 transition hover:border-brand hover:bg-brand-50 hover:text-brand">+ {humanize(type)}</button>
        ))}
      </div>
    </div>
  );
}
