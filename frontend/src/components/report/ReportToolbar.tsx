"use client";

import Link from "next/link";
import { useState, type ReactNode } from "react";
import { Icon } from "@/components/icons";
import { Button, buttonClasses, CopyField, StatusBadge, useToast } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import { errorMessage, post } from "@/lib/api";
import type { ReportView } from "@/lib/types";

const SHARE_DAYS = 30;

/** The PDF link; the one "Download PDF" link of a report page. */
export function DownloadPdfLink({ href, variant = "primary" }: { href: string; variant?: "primary" | "secondary" }) {
  const t = useT(reportMessages);
  return (
    <a href={href} target="_blank" rel="noreferrer" className={buttonClasses(variant)}>
      <Icon name="download" className="h-4 w-4" />{t("downloadPdf")}
    </a>
  );
}

/** Companies can hand out an expiring read-only link (same endpoint as the company inspection page). */
function ShareAction({ reportId }: { reportId: string }) {
  const t = useT(reportMessages);
  const toast = useToast();
  const [link, setLink] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  async function share() {
    setBusy(true);
    try {
      setLink((await post<{ link: string }>(`/api/reports/${reportId}/share-links`, { days: SHARE_DAYS })).link);
      toast.notify({ tone: "success", title: t("shareCreated") });
    } catch (e) { toast.notify({ tone: "error", title: errorMessage(e) }); } finally { setBusy(false); }
  }
  return (
    <>
      <Button variant="secondary" icon="share" loading={busy} onClick={() => void share()}>{t("shareReport")}</Button>
      {link && <div className="w-full basis-full"><CopyField label={t("shareLinkLabel")} value={link} /></div>}
    </>
  );
}

/**
 * Action bar above the document: back link, status, and the actions (PDF, share) — kept apart from the content.
 * Example: `<ReportToolbar report={data} backHref="/tenant" backLabel="My inspections" />`
 */
export function ReportToolbar({ report, backHref, backLabel, extra }: { report: ReportView; backHref?: string; backLabel?: string; extra?: ReactNode }) {
  const t = useT(reportMessages);
  return (
    <div className="mb-5 flex flex-wrap items-center gap-x-3 gap-y-3">
      {backHref && (
        <Link href={backHref} className="inline-flex items-center gap-1 text-label font-medium text-ink-3 hover:text-brand">
          <Icon name="arrowLeft" className="h-4 w-4" />{backLabel ?? t("back")}
        </Link>
      )}
      <span className="hidden h-4 w-px bg-line-strong sm:block" aria-hidden="true" />
      <span className="inline-flex items-center gap-2 text-label text-ink-3">{t("status")} <StatusBadge value={report.inspectionStatus} /></span>
      <div className="ml-auto flex flex-wrap items-center justify-end gap-2">
        {extra}
        {report.viewerKind === "Company" && <ShareAction reportId={report.reportId} />}
        {report.pdfUrl && <DownloadPdfLink href={report.pdfUrl} />}
      </div>
    </div>
  );
}
