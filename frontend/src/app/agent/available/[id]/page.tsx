"use client";

import { useParams } from "next/navigation";
import { AcceptPanel } from "@/components/AcceptPanel";
import { EmptyState, ErrorBanner, LinkButton, PageHeader, Skeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

export default function AvailableDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const t = useT(agentMessages);
  const { humanize } = useFormatters();
  const { data, error, errorStatus, loading } = useApi<AvailableInspection>(`/api/agent/available/${id}`);
  const title = data ? t("availableDetailTitle", { type: humanize(data.inspectionType), city: data.city }) : t("inspectionFallbackTitle");
  return (
    <div className="mx-auto max-w-narrow">
      <PageHeader eyebrow={t("acceptTitle")} title={title} back={{ href: "/agent/available", label: t("availableTitle") }} />
      {loading ? <Skeleton className="h-80" /> : errorStatus === 404 ? (
        // A 404 means someone else took it (or it was withdrawn); API messages are localized, so match on the status.
        <EmptyState icon="clock" title={t("noLongerAvailable")} action={<LinkButton href="/agent/available" variant="secondary">{t("availableTitle")}</LinkButton>} />
      ) : error ? <ErrorBanner message={error} /> : data && <AcceptPanel item={data} />}
    </div>
  );
}
