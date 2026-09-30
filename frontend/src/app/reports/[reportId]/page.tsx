"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { Brand } from "@/components/AppShell";
import { ReportBody, ReportMeta } from "@/components/ReportView";
import { Badge, ErrorBanner, Loading } from "@/components/ui";
import { homeFor, useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { ReportView } from "@/lib/types";

export default function ReportPage() {
  const { reportId } = useParams<{ reportId: string }>();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();
  const { data, error, loading } = useApi<ReportView>(user ? `/api/reports/${reportId}` : null);

  useEffect(() => { if (!authLoading && !user) router.replace(`/login?next=/reports/${reportId}`); }, [authLoading, user, router, reportId]);
  if (authLoading || !user) return <Loading />;

  return (
    <div className="min-h-screen">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-5xl items-center justify-between gap-3 px-4">
          <Brand />
          <div className="flex items-center gap-2">
            {data?.pdfUrl && <a href={data.pdfUrl} target="_blank" rel="noreferrer" className="rounded-lg bg-brand px-4 py-2 text-sm font-medium text-white">Download PDF</a>}
            <Link href={data?.viewerKind === "Tenant" ? `/tenant/inspections/${data.inspectionId}` : homeFor(user)} className="text-sm text-brand hover:underline">Back</Link>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-6">
        <ErrorBanner message={error} />
        {loading && !data ? <Loading /> : data && (
          <>
            <div className="mb-4 flex flex-wrap items-center gap-3"><h1 className="text-2xl font-semibold">Report {data.reportNumber}</h1><Badge value={data.inspectionStatus} /></div>
            <div className="grid gap-4 lg:grid-cols-[1fr_300px]">
              <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} observations={data.observations} />
              <div><ReportMeta report={data} /></div>
            </div>
          </>
        )}
      </main>
    </div>
  );
}
