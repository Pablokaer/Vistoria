"use client";

import { useState } from "react";
import { Button, Textarea } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { tenantMessages } from "@/i18n/messages/tenant";

/**
 * "+ Add observation about this room" under each room of the report. Collapsed by default so the document stays
 * readable; `onSubmit` resolves true when the observation was saved.
 */
export function RoomObservation({ busy, onSubmit }: { busy: boolean; onSubmit: (text: string) => Promise<boolean> }) {
  const t = useT(tenantMessages);
  const [text, setText] = useState<string | null>(null);
  if (text === null) {
    return <Button variant="ghost" size="sm" icon="message" className="mt-3 -ml-2.5" onClick={() => setText("")}>{t("addRoomObservation")}</Button>;
  }
  return (
    <div className="mt-4 space-y-2 rounded-md border border-line bg-surface-2 p-3 animate-fade-in">
      <Textarea rows={3} autoFocus value={text} onChange={(e) => setText(e.target.value)} placeholder={t("roomObservationPlaceholder")} />
      <div className="flex gap-2">
        <Button size="sm" loading={busy} disabled={!text.trim()} onClick={() => void onSubmit(text).then((ok) => { if (ok) setText(null); })}>{t("addObservation")}</Button>
        <Button size="sm" variant="secondary" onClick={() => setText(null)}>{t("cancel")}</Button>
      </div>
    </div>
  );
}
