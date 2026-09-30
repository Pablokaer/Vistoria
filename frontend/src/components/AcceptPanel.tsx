"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icons";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { agentMessages } from "@/i18n/messages/agent";
import { ApiError, errorMessage, post } from "@/lib/api";
import type { AvailableInspection } from "@/lib/types";
import { Button, DefinitionList, ErrorBanner, InspectionTypeTag, PropertyThumb, useToast, VisibilityTag } from "./ui";

/** Accept call + the "someone was faster" case (409/422), then straight to the inspection overview. */
function useAccept(id: string) {
  const router = useRouter();
  const t = useT(agentMessages);
  const toast = useToast();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const accept = async () => {
    setBusy(true);
    setError(null);
    try {
      await post(`/api/agent/inspections/${id}/accept`);
      toast.notify({ tone: "success", title: t("acceptedToast") });
      router.replace(`/agent/inspections/${id}`);
    } catch (e) {
      setError(e instanceof ApiError && (e.status === 409 || e.status === 422) ? t("acceptConflict") : errorMessage(e));
      setBusy(false);
    }
  };
  return { busy, error, accept };
}

/**
 * Decision screen for an open inspection: what, where (area only — the address is revealed on accept), when,
 * rooms, and one prominent accept action (sticky at the bottom on phones).
 * `insideShell`: the phone tab bar (h-14) sits under the sticky bar; standalone pages (private invite) have none.
 */
export function AcceptPanel({ item, insideShell = true }: { item: AvailableInspection; insideShell?: boolean }) {
  const t = useT(agentMessages);
  const { formatDate, humanize } = useFormatters();
  const { busy, error, accept } = useAccept(item.id);
  const facts: [string, string][] = [
    [t("whereLabel"), `${item.city} ${item.postcodeArea}`.trim()], [t("property"), humanize(item.propertyType)],
    [t("whenLabel"), item.scheduledDate ? formatDate(item.scheduledDate) : t("notScheduled")],
    ...(item.acceptBy ? [[t("acceptBy"), formatDate(item.acceptBy)] as [string, string]] : []),
    [t("baseline"), item.hasComparison ? t("baselineAvailable") : "—"],
  ];
  return (
    <div className="space-y-4 pb-24 sm:pb-0">
      <ErrorBanner message={error} />
      <section className="overflow-hidden rounded-lg border border-line bg-surface shadow-card">
        <div className="flex items-start gap-4 border-b border-line p-4 sm:p-5">
          <PropertyThumb type={item.inspectionType} size="lg" />
          <div className="min-w-0">
            <div className="mb-1 flex flex-wrap items-center gap-2"><InspectionTypeTag type={item.inspectionType} /><VisibilityTag visibility={item.visibility} /></div>
            <p className="text-card-title font-semibold text-ink">{item.companyName}</p>
          </div>
        </div>
        <div className="p-4 sm:p-5"><DefinitionList columns={2} items={facts} /></div>
        <div className="border-t border-line p-4 sm:p-5">
          <h3 className="mb-2 text-label font-semibold text-ink">{t("roomsCount", { count: item.roomCount })}</h3>
          <ul className="flex flex-wrap gap-1.5">{item.roomNames.map((r) => <li key={r} className="rounded-sm bg-neutral-50 px-2 py-1 text-label text-ink-2 ring-1 ring-inset ring-line">{r}</li>)}</ul>
        </div>
      </section>
      <p className="flex gap-2 text-label text-ink-3"><Icon name="lock" className="mt-0.5 h-4 w-4 shrink-0" />{t("acceptHint")}</p>
      <div className={`fixed inset-x-0 ${insideShell ? "bottom-14" : "bottom-0"} z-20 border-t border-line bg-surface/95 p-3 backdrop-blur sm:static sm:border-0 sm:bg-transparent sm:p-0`}>
        <Button size="lg" icon="check" className="w-full sm:w-auto" loading={busy} onClick={() => void accept()}>{t("acceptInspection")}</Button>
      </div>
    </div>
  );
}
