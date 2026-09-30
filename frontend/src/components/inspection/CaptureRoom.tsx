"use client";

import { useCallback, useState } from "react";
import { BaselinePanel } from "@/components/ComparisonPanel";
import { Icon } from "@/components/icons";
import { PhotoUploader } from "@/components/PhotoUploader";
import { Button, IconButton, useConfirm } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { captureMessages } from "@/i18n/messages/capture";
import { del, errorMessage, post, put } from "@/lib/api";
import type { Defect, RoomDetail } from "@/lib/types";
import { RoomSection } from "./RoomSection";

export type OnBusy = (key: string, busy: boolean) => void;

const photosBaseOf = (roomBase: string) => roomBase.replace(/\/rooms\/[^/]+$/, "/photos");

function DefectCapture({ defect, index, base, editable, reload, onBusy, onRemove }: {
  defect: Defect; index: number; base: string; editable: boolean; reload: () => Promise<void>; onBusy: OnBusy; onRemove: () => void;
}) {
  const t = useT(captureMessages);
  const onDefectBusy = useCallback((b: boolean) => onBusy(`defect:${defect.id}`, b), [onBusy, defect.id]);
  return (
    <div data-testid="defect-card" className="rounded-md border border-warning-500/35 bg-warning-50/40 p-3">
      <div className="mb-2 flex items-center justify-between gap-2">
        <h4 className="flex items-center gap-2 text-label font-semibold text-ink">
          <span className="tabular grid h-6 w-6 place-items-center rounded-sm bg-warning-50 text-caption text-warning-700 ring-1 ring-inset ring-warning-700/20">{index + 1}</span>
          {t("defectNumber", { number: index + 1 })}
        </h4>
        {editable && <IconButton icon="trash" label={t("removeDefect")} onClick={onRemove} className="hover:text-danger-700" />}
      </div>
      <PhotoUploader compact label={t("defectPhotos")} uploadUrl={`${base}/photos`} deleteUrlBase={photosBaseOf(base)}
        photos={defect.photos} mediaType="Defect" defectId={defect.id} editable={editable} onChanged={reload} onBusyChange={onDefectBusy} />
    </div>
  );
}

/** Adds/removes defects of a room; removing the last one also clears "defects found" (it would block completion). */
function useDefectActions(base: string, onRoom: (r: RoomDetail) => void) {
  const t = useT(captureMessages);
  const [confirm, confirmDialog] = useConfirm();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function run(fn: () => Promise<void>) {
    setBusy(true);
    setError(null);
    try { await fn(); } catch (e) { setError(errorMessage(e)); } finally { setBusy(false); }
  }
  const addDefect = () => run(async () => onRoom(await post<RoomDetail>(`${base}/defects`, { description: null, location: null })));
  const removeDefect = async (d: Defect) => {
    if (!(await confirm({ title: t("confirmRemoveDefect"), confirmLabel: t("removeDefect"), danger: true }))) return;
    await run(async () => {
      let updated = await del<RoomDetail>(`${base}/defects/${d.id}`);
      if (updated.defects.length === 0 && updated.defectsFound)
        updated = await put<RoomDetail>(base, { finalDescription: updated.finalDescription, defectsFound: false, agentNotes: updated.agentNotes });
      onRoom(updated);
    });
  };
  return { addDefect, removeDefect, busy, error, confirmDialog };
}

/** Step 1 for one room: its general photos, then its defects and their photos. No text is written here. */
export function CaptureRoom({ room, base, onRoom, reload, onBusy }: {
  room: RoomDetail; base: string; onRoom: (r: RoomDetail) => void; reload: () => Promise<void>; onBusy: OnBusy;
}) {
  const t = useT(captureMessages);
  const { addDefect, removeDefect, busy, error, confirmDialog } = useDefectActions(base, onRoom);
  const onGeneralBusy = useCallback((b: boolean) => onBusy(`${room.id}:general`, b), [onBusy, room.id]);
  const photoCount = <span className="tabular inline-flex items-center gap-1 text-caption text-ink-3"><Icon name="image" className="h-4 w-4" />{room.generalPhotos.length}</span>;
  return (
    <RoomSection room={room} aside={photoCount}>
      {room.comparison?.baseline && (
        <details className="group rounded-md">
          <summary className="flex cursor-pointer list-none items-center gap-1.5 text-label font-medium text-movein">
            <Icon name="doorIn" className="h-4 w-4" />{t("showMoveInRecord")}<Icon name="chevronDown" className="h-4 w-4 transition group-open:rotate-180" />
          </summary>
          <div className="mt-2"><BaselinePanel room={room} /></div>
        </details>
      )}
      <PhotoUploader uploadUrl={`${base}/photos`} deleteUrlBase={photosBaseOf(base)} photos={room.generalPhotos} mediaType="General"
        editable={room.editable} onChanged={reload} onBusyChange={onGeneralBusy} label={t("roomPhotos")} />
      <div className="border-t border-line pt-4">
        <div className="mb-2 flex items-center justify-between gap-3">
          <h3 className="text-label font-semibold text-ink-2">{t("defectsCount", { count: room.defects.length })}</h3>
          {room.editable && <Button variant="secondary" size="sm" loading={busy} onClick={() => void addDefect()}>{t("addDefect")}</Button>}
        </div>
        {room.defects.length === 0 && <p className="text-label text-ink-3">{t("noDefectsYet")}</p>}
        <div className="space-y-3">
          {room.defects.map((d, i) => (
            <DefectCapture key={d.id} defect={d} index={i} base={base} editable={room.editable} reload={reload} onBusy={onBusy} onRemove={() => void removeDefect(d)} />
          ))}
        </div>
        {error && <p role="alert" className="mt-2 text-label text-danger-700">{error}</p>}
      </div>
      {confirmDialog}
    </RoomSection>
  );
}
