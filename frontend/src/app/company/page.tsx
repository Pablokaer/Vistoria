"use client";

import Link from "next/link";
import { Badge, Card, EmptyState, ErrorBanner, LinkButton, Loading, PageHeader, Stat } from "@/components/ui";
import { useAuth } from "@/lib/auth";
import { formatDateTime, humanize } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { CompanyDashboard } from "@/lib/types";

export default function CompanyDashboardPage() {
  const { user } = useAuth();
  const { data, error, loading, reload } = useApi<CompanyDashboard>("/api/company/dashboard");

  return (
    <>
      <PageHeader title={user?.company?.companyName ?? "Dashboard"} subtitle="Overview of your properties and inspections"
        actions={<><LinkButton href="/company/properties/new" variant="secondary">Add property</LinkButton><LinkButton href="/company/inspections/new">New inspection</LinkButton></>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <Loading /> : data && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
            <Stat label="Properties" value={data.properties} href="/company/properties" />
            <Stat label="Open" value={data.open + data.draft} />
            <Stat label="Assigned" value={data.assigned} />
            <Stat label="In progress" value={data.inProgress + data.inReview} />
            <Stat label="Awaiting tenant" value={data.awaitingTenant} />
            <Stat label="Completed" value={data.completed} />
          </div>
          {data.disputed > 0 && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">{data.disputed} inspection report(s) disputed by tenants.</div>
          )}
          <Card title="Recent inspections">
            {data.recentInspections.length === 0 ? (
              <EmptyState title="No inspections yet">Add a property with its rooms, then create an inspection.</EmptyState>
            ) : (
              <ul className="divide-y divide-slate-100">
                {data.recentInspections.map((i) => (
                  <li key={i.id}>
                    <Link href={`/company/inspections/${i.id}`} className="flex flex-wrap items-center justify-between gap-2 py-3 hover:bg-slate-50">
                      <span>
                        <span className="block font-medium text-slate-900">{i.propertyAddress}</span>
                        <span className="text-sm text-slate-600">{humanize(i.inspectionType)} · {i.agentName ?? "No agent yet"} · {formatDateTime(i.updatedAt)}</span>
                      </span>
                      <Badge value={i.status} />
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      )}
    </>
  );
}
