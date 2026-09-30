"use client";

import { useParams } from "next/navigation";
import { Brand } from "@/components/AppShell";
import { Icon } from "@/components/icons";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { DownloadPdfLink, ReportBody, ReportContents } from "@/components/ReportView";
import { ReportPageSkeleton } from "@/components/report/ReportPageSkeleton";
import { EmptyState } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import { useApi } from "@/lib/hooks";
import type { ReportView } from "@/lib/types";

/** Read-only report through an expiring share link (no account needed, no personal emails). */
export default function SharedReportPage() {
  const { token } = useParams<{ token: string }>();
  const t = useT(reportMessages);
  const { data, error, loading } = useApi<ReportView>(`/api/shared/reports/${encodeURIComponent(token)}`);
  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-30 border-b border-line bg-surface/95 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-page items-center justify-between gap-3 px-4 sm:px-6">
          <Brand alwaysShowName />
          <div className="flex items-center gap-2">
            <LanguageSwitcher />
            {data?.pdfUrl && <DownloadPdfLink href={data.pdfUrl} />}
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-page px-4 py-6 sm:px-6">
        {loading ? <ReportPageSkeleton /> : error ? <EmptyState icon="lock" title={t("linkInvalid")} /> : data && (
          <>
            <p className="mb-4 inline-flex items-center gap-1.5 text-label text-ink-3"><Icon name="eye" className="h-4 w-4" />{t("sharedNotice", { company: data.snapshot.company.name })}</p>
            <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_16rem]">
              <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} />
              <aside className="lg:sticky lg:top-20"><ReportContents snapshot={data.snapshot} /></aside>
            </div>
          </>
        )}
      </main>
    </div>
  );
}
