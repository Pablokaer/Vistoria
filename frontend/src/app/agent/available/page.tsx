"use client";

import { useMemo, useState } from "react";
import { applyQuery, EMPTY_QUERY, isFiltered, type MarketplaceQuery } from "@/components/agent/marketplace";
import { MarketplaceFilters } from "@/components/agent/MarketplaceFilters";
import { InspectionCard, InspectionCardGrid } from "@/components/inspection/InspectionCard";
import { fromAvailable } from "@/components/inspection/inspectionCardModels";
import { Button, EmptyState, ErrorBanner, PageHeader, Skeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { marketplaceMessages } from "@/i18n/messages/agent-marketplace";
import { inspectionListMessages } from "@/i18n/messages/inspection-lists";
import { useApi } from "@/lib/hooks";
import type { AvailableInspection } from "@/lib/types";

function GridSkeleton() {
  return <InspectionCardGrid dense>{[0, 1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-44" />)}</InspectionCardGrid>;
}

/** Marketplace of open public inspections: search, filter and sort client-side over the API's full list. */
export default function AvailablePage() {
  const t = useT(marketplaceMessages);
  const cardText = useT(inspectionListMessages);
  const formatters = useFormatters();
  const { data, error, loading, reload } = useApi<AvailableInspection[]>("/api/agent/available");
  const [query, setQuery] = useState<MarketplaceQuery>(EMPTY_QUERY);
  const all = useMemo(() => data ?? [], [data]);
  const shown = useMemo(() => applyQuery(all, query), [all, query]);
  return (
    <>
      <PageHeader title={t("title")} subtitle={t("subtitle")}
        actions={<Button variant="secondary" icon="refresh" onClick={() => void reload()} loading={loading}>{t("refresh")}</Button>} />
      <ErrorBanner message={error} onRetry={reload} />
      {loading && !data ? <GridSkeleton /> : all.length === 0 ? (
        <EmptyState icon="search" title={t("noneTitle")}>{t("noneBody")}</EmptyState>
      ) : (
        <div className="space-y-4">
          <MarketplaceFilters items={all} query={query} onChange={setQuery} />
          <p className="tabular text-label text-ink-3" aria-live="polite">
            {isFiltered(query) ? t("results", { count: shown.length, total: all.length }) : t("resultsAll", { count: all.length })}
          </p>
          {shown.length === 0 ? (
            <EmptyState icon="filter" title={t("noMatchTitle")}
              action={<Button variant="secondary" onClick={() => setQuery({ ...EMPTY_QUERY, sort: query.sort })}>{t("clearFilters")}</Button>}>
              {t("noMatchBody")}
            </EmptyState>
          ) : (
            <InspectionCardGrid dense>{shown.map((i) => <InspectionCard key={i.id} model={fromAvailable(i, cardText, formatters)} />)}</InspectionCardGrid>
          )}
        </div>
      )}
    </>
  );
}
