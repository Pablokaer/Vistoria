"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";
import { AppShell } from "@/components/AppShell";
import { ReportBody, ReportMeta, ReportToolbar } from "@/components/ReportView";
import { ReportPageSkeleton } from "@/components/report/ReportPageSkeleton";
import { ErrorBanner, PageSkeleton } from "@/components/ui";
import { useT } from "@/i18n/I18nProvider";
import { reportMessages } from "@/i18n/messages/report";
import { homeFor, useAuth } from "@/lib/auth";
import { useApi } from "@/lib/hooks";
import type { ReportView, Role } from "@/lib/types";

/** Where "Back" leads: tenants return to their review page, everyone else to their home. */
function backHref(report: ReportView | null, fallback: string): string {
  return report?.viewerKind === "Tenant" ? `/tenant/inspections/${report.inspectionId}` : fallback;
}

/** A finalized report for any signed-in viewer, inside the viewer's own app shell. */
export default function ReportPage() {
  const { reportId } = useParams<{ reportId: string }>();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();
  const t = useT(reportMessages);
  const { data, error, loading } = useApi<ReportView>(user ? `/api/reports/${reportId}` : null);

  useEffect(() => { if (!authLoading && !user) router.replace(`/login?next=/reports/${reportId}`); }, [authLoading, user, router, reportId]);
  if (authLoading || !user) return <div className="mx-auto max-w-page px-4 py-8"><PageSkeleton /></div>;
  const role: Role = user.roles[0] ?? "Tenant";

  return (
    <AppShell role={role} wide>
      <ErrorBanner message={error} />
      {loading && !data ? <ReportPageSkeleton /> : data && (
        <>
          <ReportToolbar report={data} backHref={backHref(data, homeFor(user))} backLabel={t("back")} />
          <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_17rem]">
            <ReportBody snapshot={data.snapshot} photoUrls={data.photoUrls} observations={data.observations} />
            <aside className="space-y-4 xl:sticky xl:top-6"><ReportMeta report={data} /></aside>
          </div>
        </>
      )}
    </AppShell>
  );
}
