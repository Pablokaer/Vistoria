"use client";

import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyFormMessages } from "@/i18n/messages/company-forms";
import { ROOM_TYPES, type RoomType } from "@/lib/types";
import { Button, Input, Select } from "./ui";

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
    <div className="space-y-2">
      {rooms.map((room, i) => (
        <div key={room.key} className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 p-2 sm:flex-nowrap">
          <span className="w-6 text-center text-sm text-slate-400">{i + 1}</span>
          <Select className="sm:w-44" value={room.roomType} onChange={(e) => update(room.key, { roomType: e.target.value as RoomType })}>
            {ROOM_TYPES.map((type) => <option key={type} value={type}>{humanize(type)}</option>)}
          </Select>
          <Input className="min-w-0 flex-1" value={room.name} onChange={(e) => update(room.key, { name: e.target.value })} aria-label={t("roomName")} required />
          <div className="flex gap-1">
            <Button type="button" variant="ghost" disabled={i === 0} onClick={() => move(i, -1)} aria-label={t("moveUp")}>↑</Button>
            <Button type="button" variant="ghost" disabled={i === rooms.length - 1} onClick={() => move(i, 1)} aria-label={t("moveDown")}>↓</Button>
            <Button type="button" variant="ghost" onClick={() => onChange(rooms.filter((r) => r.key !== room.key))} aria-label={t("removeRoom")}>✕</Button>
          </div>
        </div>
      ))}
      <div className="flex flex-wrap gap-2 pt-1">
        {QUICK_ADD_TYPES.map((type) => (
          <Button key={type} type="button" variant="secondary" onClick={() => add(type)}>+ {humanize(type)}</Button>
        ))}
      </div>
    </div>
  );
}
