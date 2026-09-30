"use client";

import { useParams } from "next/navigation";
import { AcceptPanel } from "@/components/AcceptPanel";
import { ErrorBanner, Loading, PageHeader } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

export default function AvailableDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const t = useT(agentMessages);
  const { humanize } = useFormatters();
  const { data, error, errorStatus, loading } = useApi<AvailableInspection>(`/api/agent/available/${id}`);
  // A 404 means someone else took it (or it was withdrawn); API messages are localized, so match on the status.
  const shownError = errorStatus === 404 ? t("noLongerAvailable") : error;
  return (
    <div className="mx-auto max-w-2xl">
      <PageHeader title={data ? t("availableDetailTitle", { type: humanize(data.inspectionType), city: data.city }) : t("inspectionFallbackTitle")}
        back={{ href: "/agent/available", label: t("availableTitle") }} />
      {loading ? <Loading /> : error ? <ErrorBanner message={shownError} /> : data && <AcceptPanel item={data} />}
    </div>
  );
}
