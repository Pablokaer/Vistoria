"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, errorMessage, post } from "@/lib/api";
import { formatDate, humanize } from "@/lib/format";
import type { AvailableInspection } from "@/lib/types";
import { Button, Card, DefinitionList, ErrorBanner } from "./ui";

export function AcceptPanel({ item }: { item: AvailableInspection }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function accept() {
    setBusy(true);
    setError(null);
    try {
      await post(`/api/agent/inspections/${item.id}/accept`);
      router.replace(`/agent/inspections/${item.id}`);
    } catch (e) {
      setError(e instanceof ApiError && (e.status === 409 || e.status === 422) ? "Another inspector accepted this inspection just before you." : errorMessage(e));
      setBusy(false);
    }
  }

  return (
    <Card>
      <ErrorBanner message={error} />
      <DefinitionList items={[
        ["Type", humanize(item.inspectionType)], ["Company", item.companyName], ["Area", `${item.city} ${item.postcodeArea}`],
        ["Property", humanize(item.propertyType)], ["Scheduled", formatDate(item.scheduledDate)],
        ...(item.acceptBy ? [["Accept by", formatDate(item.acceptBy)] as [string, string]] : []),
        ["Baseline", item.hasComparison ? "Move In report available for comparison" : "—"],
      ]} />
      <h3 className="mt-5 mb-2 text-sm font-semibold">Rooms ({item.roomCount})</h3>
      <ul className="flex flex-wrap gap-2">{item.roomNames.map((r) => <li key={r} className="rounded-full bg-slate-100 px-3 py-1 text-sm">{r}</li>)}</ul>
      <Button size="lg" className="mt-6 w-full sm:w-auto" loading={busy} onClick={() => void accept()}>Accept inspection</Button>
      <p className="mt-2 text-xs text-slate-500">The full address, instructions and tenant names become visible after you accept.</p>
    </Card>
  );
}
