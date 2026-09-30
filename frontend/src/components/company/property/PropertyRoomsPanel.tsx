"use client";

import { useState } from "react";
import { Button, Card, IconButton, Input, Select, useConfirm } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import { del, post, put } from "@/lib/api";
import { ROOM_TYPES, type Property, type PropertyRoom, type RoomType } from "@/lib/types";
import type { Run } from "./PropertyDetailsPanel";

function RoomTypeSelect({ value, onChange }: { value: RoomType; onChange: (t: RoomType) => void }) {
  const { humanize } = useFormatters();
  return <Select className="sm:w-44" value={value} onChange={(e) => onChange(e.target.value as RoomType)}>{ROOM_TYPES.map((rt) => <option key={rt} value={rt}>{humanize(rt)}</option>)}</Select>;
}

function RenameRoomForm({ room, onSubmit, onCancel }: { room: PropertyRoom; onSubmit: (type: RoomType, name: string) => void; onCancel: () => void }) {
  const t = useT(companyPropertyMessages);
  const [name, setName] = useState(room.name);
  const [type, setType] = useState<RoomType>(room.roomType);
  return (
    <form className="flex w-full flex-wrap gap-2" onSubmit={(e) => { e.preventDefault(); onSubmit(type, name); }}>
      <RoomTypeSelect value={type} onChange={setType} />
      <Input className="min-w-0 flex-1" value={name} onChange={(e) => setName(e.target.value)} required aria-label={t("roomNamePlaceholder")} />
      <Button type="submit" size="sm">{t("save")}</Button><Button type="button" size="sm" variant="subtle" onClick={onCancel}>{t("cancel")}</Button>
    </form>
  );
}

function AddRoomForm({ onAdd }: { onAdd: (type: RoomType, name: string) => Promise<void> }) {
  const t = useT(companyPropertyMessages);
  const [type, setType] = useState<RoomType>("Bedroom");
  const [name, setName] = useState("");
  return (
    <form className="flex flex-wrap gap-2 border-t border-line bg-surface-2 px-4 py-3 sm:px-5" onSubmit={(e) => { e.preventDefault(); void onAdd(type, name).then(() => setName("")); }}>
      <RoomTypeSelect value={type} onChange={setType} />
      <Input className="min-w-0 flex-1" placeholder={t("roomNamePlaceholder")} value={name} onChange={(e) => setName(e.target.value)} required />
      <Button type="submit" variant="secondary" icon="plus">{t("addRoom")}</Button>
    </form>
  );
}

/** The property's live room list (walking order). Edits affect future inspections only. */
export function PropertyRoomsPanel({ property, onChanged, run }: { property: Property; onChanged: (p: Property) => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { humanize } = useFormatters();
  const [confirm, dialog] = useConfirm();
  const [editing, setEditing] = useState<string | null>(null);
  const base = `/api/properties/${property.id}/rooms`;
  const rooms = property.rooms;

  const move = (index: number, delta: number) => run(async () => {
    const ids = rooms.map((r) => r.id);
    const [item] = ids.splice(index, 1);
    ids.splice(index + delta, 0, item);
    onChanged(await put<Property>(`${base}/order`, { roomIds: ids }));
  });
  const rename = (room: PropertyRoom, roomType: RoomType, name: string) =>
    run(async () => { onChanged(await put<Property>(`${base}/${room.id}`, { roomType, name })); setEditing(null); });
  const remove = async (room: PropertyRoom) => {
    if (await confirm({ title: t("confirmRemoveRoom", { name: room.name }), description: t("confirmRemoveRoomHint"), confirmLabel: t("remove"), danger: true }))
      await run(async () => onChanged(await del<Property>(`${base}/${room.id}`)));
  };

  return (
    <Card flush title={t("rooms", { count: rooms.length })} description={t("roomsNotice")}>
      {dialog}
      <ol className="divide-y divide-line">
        {rooms.map((r, i) => (
          <li key={r.id} className="flex flex-wrap items-center gap-2 px-4 py-2 sm:px-5">
            {editing === r.id ? <RenameRoomForm room={r} onCancel={() => setEditing(null)} onSubmit={(type, name) => void rename(r, type, name)} /> : (
              <>
                <span className="tabular w-6 text-caption font-semibold text-ink-3">{i + 1}</span>
                <span className="min-w-0 flex-1 text-body"><span className="font-medium text-ink">{r.name}</span> <span className="text-ink-3">· {humanize(r.roomType)}</span></span>
                <IconButton icon="chevronDown" className="rotate-180" disabled={i === 0} onClick={() => void move(i, -1)} label={t("moveUp")} />
                <IconButton icon="chevronDown" disabled={i === rooms.length - 1} onClick={() => void move(i, 1)} label={t("moveDown")} />
                <IconButton icon="edit" onClick={() => setEditing(r.id)} label={t("rename")} />
                <IconButton icon="trash" className="hover:text-danger-700" onClick={() => void remove(r)} label={t("remove")} />
              </>
            )}
          </li>
        ))}
      </ol>
      <AddRoomForm onAdd={(roomType, name) => run(async () => onChanged(await post<Property>(base, { roomType, name })))} />
    </Card>
  );
}
