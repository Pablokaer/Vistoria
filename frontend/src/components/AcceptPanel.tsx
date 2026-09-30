"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { ApiError, errorMessage, post } from "@/lib/api";
import type { AvailableInspection } from "@/lib/types";
import { Button, Card, DefinitionList, ErrorBanner } from "./ui";

export function AcceptPanel({ item }: { item: AvailableInspection }) {
  const router = useRouter();
  const t = useT(agentMessages);
  const { formatDate, humanize } = useFormatters();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function accept() {
    setBusy(true);
    setError(null);
    try {
      await post(`/api/agent/inspections/${item.id}/accept`);
      router.replace(`/agent/inspections/${item.id}`);
    } catch (e) {
      setError(e instanceof ApiError && (e.status === 409 || e.status === 422) ? t("acceptConflict") : errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <Card>
      <ErrorBanner message={error} />
      <DefinitionList items={[
        [t("type"), humanize(item.inspectionType)], [t("company"), item.companyName], [t("area"), `${item.city} ${item.postcodeArea}`],
        [t("property"), humanize(item.propertyType)], [t("scheduled"), formatDate(item.scheduledDate)],
        ...(item.acceptBy ? [[t("acceptBy"), formatDate(item.acceptBy)] as [string, string]] : []),
        [t("baseline"), item.hasComparison ? t("baselineAvailable") : "—"],
      ]} />
      <h3 className="mt-5 mb-2 text-sm font-semibold">{t("roomsCount", { count: item.roomCount })}</h3>
      <ul className="flex flex-wrap gap-2">{item.roomNames.map((r) => <li key={r} className="rounded-full bg-slate-100 px-3 py-1 text-sm">{r}</li>)}</ul>
      <Button size="lg" className="mt-6 w-full sm:w-auto" loading={busy} onClick={() => void accept()}>{t("acceptInspection")}</Button>
      <p className="mt-2 text-xs text-slate-500">{t("acceptHint")}</p>
    </Card>
  );
}
