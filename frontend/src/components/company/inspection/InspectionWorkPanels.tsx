"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { Button, Card, CopyField, DefinitionList, LinkButton, ProgressBar, StatusBadge, useToast } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyInspectionMessages } from "@/i18n/messages/company-inspection";
import { post } from "@/lib/api";
import type { InspectionDetails, RoomProgress } from "@/lib/types";

function RoomRow({ room }: { room: RoomProgress }) {
  const t = useT(companyInspectionMessages);
  return (
    <li className="flex items-center gap-3 px-4 py-2.5 sm:px-5">
      <span className="tabular w-5 text-caption font-semibold text-ink-3">{room.sequence}</span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-body font-medium text-ink">{room.name}</span>
        <span className="flex gap-3 text-caption text-ink-3">
          <span className="inline-flex items-center gap-1"><Icon name="camera" className="h-3.5 w-3.5" />{t("photos", { count: room.generalPhotoCount })}</span>
          {room.defectCount > 0 && <span className="inline-flex items-center gap-1 text-warning-700"><Icon name="alert" className="h-3.5 w-3.5" />{t("defects", { count: room.defectCount })}</span>}
        </span>
      </span>
      <StatusBadge value={room.status} />
    </li>
  );
}

/** Room-by-room progress as the inspector records it. */
export function RoomsProgressPanel({ inspection: i }: { inspection: InspectionDetails }) {
  const t = useT(companyInspectionMessages);
  return (
    <Card flush title={t("progress")}>
      <div className="border-b border-line px-4 py-3 sm:px-5"><ProgressBar value={i.roomsCompleted} total={i.rooms.length} /></div>
      {i.rooms.length === 0 ? <p className="px-4 py-4 text-label text-ink-3 sm:px-5">{t("roomsCapturedOnPublish")}</p>
        : <ul className="divide-y divide-line sm:grid sm:grid-cols-2 sm:divide-y-0 sm:[&>li]:border-b sm:[&>li]:border-line">{i.rooms.map((r) => <RoomRow key={r.id} room={r} />)}</ul>}
    </Card>
  );
}

/** Report metadata, open/share actions. Share links are read-only and expire after 30 days. */
export function ReportPanel({ report }: { report: NonNullable<InspectionDetails["report"]> }) {
  const t = useT(companyInspectionMessages);
  const { formatDateTime } = useFormatters();
  const toast = useToast();
  const [busy, setBusy] = useState(false);
  const [shareLink, setShareLink] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const share = async () => {
    setBusy(true);
    setError(null);
    try {
      setShareLink((await post<{ link: string }>(`/api/reports/${report.reportId}/share-links`, { days: 30 })).link);
      toast.notify({ tone: "success", title: t("shareLinkCreated") });
    } catch (e) { setError(e instanceof Error ? e.message : String(e)); } finally { setBusy(false); }
  };
  return (
    <Card title={t("report")} actions={<>
      <Button variant="secondary" size="sm" icon="share" loading={busy} onClick={() => void share()}>{t("createShareLink")}</Button>
      <LinkButton href={`/reports/${report.reportId}`} size="sm" icon="file">{t("viewReport")}</LinkButton>
    </>}>
      <DefinitionList columns={2} items={[[t("reportNumber"), report.reportNumber], [t("version"), `v${report.latestVersion}`], [t("generated"), formatDateTime(report.generatedAt)]]} />
      {error && <p role="alert" className="mt-3 text-label text-danger-700">{error}</p>}
      {shareLink && <div className="mt-4 border-t border-line pt-4"><CopyField label={t("readOnlyLink")} value={shareLink} /></div>}
    </Card>
  );
}
