"use client";

import { useParams } from "next/navigation";
import { Brand } from "@/components/AppShell";
import { ReportBody } from "@/components/ReportView";
import { ErrorBanner, Loading } from "@/components/ui";
import { useApi } from "@/lib/hooks";
import type { ReportView } from "@/lib/types";

/** Read-only report through an expiring share link (no account needed, no personal emails). */
export default function SharedReportPage() {
  const { token } = useParams<{ token: string }>();
  const { data, error, loading } = useApi<ReportView>(`/api/shared/reports/${encodeURIComponent(token)}`);
  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-4xl items-center justify-between px-4">
          <Brand />
          {data?.pdfUrl && <a href={data.pdfUrl} target="_blank" rel="noreferrer" className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white">Download PDF</a>}
        </div>
      </header>
      <main className="mx-auto max-w-4xl px-4 py-6">
        {loading ? <Loading /> : error ? <ErrorBanner message="This link is invalid or has expired." /> : data && <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} />}
      </main>
    </div>
  );
}
