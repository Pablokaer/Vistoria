"use client";

import { Button, Input, Select } from "./ui";
import { humanize } from "@/lib/format";
import { ROOM_TYPES, type RoomType } from "@/lib/types";

export interface RoomDraft { key: string; roomType: RoomType; name: string }

export const DEFAULT_ROOMS: RoomDraft[] = [
  { key: "1", roomType: "LivingRoom", name: "Living Room" },
  { key: "2", roomType: "Bedroom", name: "Bedroom 1" },
  { key: "3", roomType: "Bedroom", name: "Bedroom 2" },
  { key: "4", roomType: "Kitchen", name: "Kitchen" },
  { key: "5", roomType: "Bathroom", name: "Bathroom" },
];

const defaultName = (type: RoomType, rooms: RoomDraft[]) => {
  const base = humanize(type).replace(/^./, (c) => c.toUpperCase());
  const same = rooms.filter((r) => r.roomType === type).length;
  return type === "Bedroom" || same > 0 ? `${base} ${same + 1}` : base;
};

/** Editable list of rooms used when creating a property. */
export function RoomListEditor({ rooms, onChange }: { rooms: RoomDraft[]; onChange: (rooms: RoomDraft[]) => void }) {
  const update = (key: string, patch: Partial<RoomDraft>) => onChange(rooms.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  const move = (index: number, delta: number) => {
    const next = [...rooms];
    const [item] = next.splice(index, 1);
    next.splice(index + delta, 0, item);
    onChange(next);
  };
  return (
    <div className="space-y-2">
      {rooms.map((room, i) => (
        <div key={room.key} className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 p-2 sm:flex-nowrap">
          <span className="w-6 text-center text-sm text-slate-400">{i + 1}</span>
          <Select className="sm:w-44" value={room.roomType} onChange={(e) => update(room.key, { roomType: e.target.value as RoomType })}>
            {ROOM_TYPES.map((t) => <option key={t} value={t}>{humanize(t)}</option>)}
          </Select>
          <Input className="min-w-0 flex-1" value={room.name} onChange={(e) => update(room.key, { name: e.target.value })} aria-label="Room name" required />
          <div className="flex gap-1">
            <Button type="button" variant="ghost" disabled={i === 0} onClick={() => move(i, -1)} aria-label="Move up">↑</Button>
            <Button type="button" variant="ghost" disabled={i === rooms.length - 1} onClick={() => move(i, 1)} aria-label="Move down">↓</Button>
            <Button type="button" variant="ghost" onClick={() => onChange(rooms.filter((r) => r.key !== room.key))} aria-label="Remove">✕</Button>
          </div>
        </div>
      ))}
      <div className="flex flex-wrap gap-2 pt-1">
        {(["Bedroom", "Bathroom", "Kitchen", "LivingRoom", "Garage", "Garden", "Other"] as RoomType[]).map((t) => (
          <Button key={t} type="button" variant="secondary" onClick={() => onChange([...rooms, { key: crypto.randomUUID(), roomType: t, name: defaultName(t, rooms) }])}>
            + {humanize(t)}
          </Button>
        ))}
      </div>
    </div>
  );
}
