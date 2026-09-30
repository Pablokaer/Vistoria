"use client";

import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { ReportBody } from "@/components/ReportView";
import { Button, ErrorBanner, LinkButton, Loading, Notice, PageHeader, Textarea } from "@/components/ui";
import { errorMessage, post, put } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { Review } from "@/lib/types";

export default function ReviewPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { data, error, loading, reload } = useApi<Review>(`/api/agent/inspections/${id}/review`);
  const [busy, setBusy] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [editing, setEditing] = useState<string | null>(null);
  const [text, setText] = useState("");

  async function act(name: string, fn: () => Promise<void>) {
    setBusy(name);
    setActionError(null);
    try { await fn(); } catch (e) { setActionError(errorMessage(e)); } finally { setBusy(null); }
  }

  if (loading && !data) return <Loading label="Building report preview…" />;
  if (!data) return <ErrorBanner message={error} onRetry={reload} />;
  const inReview = data.inspection.status === "Review";

  return (
    <div className="pb-28">
      <PageHeader title="Review inspection" subtitle="This is exactly what will be frozen into the report. Check every room before finalizing."
        back={{ href: `/agent/inspections/${id}`, label: "Inspection" }}
        actions={inReview && <Button variant="secondary" loading={busy === "back"} onClick={() => act("back", async () => { await post(`/api/agent/inspections/${id}/return-to-progress`); router.push(`/agent/inspections/${id}`); })}>Back to rooms</Button>} />
      <ErrorBanner message={actionError} />
      {!inReview && data.inspection.status === "InProgress" && <Notice tone="warning">Submit the inspection for review from the rooms page first.</Notice>}
      {data.blockingIssues.length > 0 && (
        <Notice tone="warning"><strong>Finalization is blocked:</strong><ul className="mt-1 list-disc pl-5">{data.blockingIssues.map((i) => <li key={i}>{i}</li>)}</ul></Notice>
      )}
      <ReportBody snapshot={data.preview} photoUrls={data.photoUrls} renderRoomExtra={(roomId) => inReview && (
        editing === roomId ? (
          <div className="mt-3 space-y-2">
            <Textarea rows={5} value={text} onChange={(e) => setText(e.target.value)} />
            <div className="flex gap-2">
              <Button loading={busy === "save"} onClick={() => act("save", async () => {
                const current = data.preview.rooms.find((r) => r.id === roomId)!;
                await put(`/api/agent/inspections/${id}/rooms/${roomId}`, { finalDescription: text, defectsFound: current.defectsFound, agentNotes: current.agentNotes });
                setEditing(null);
                await reload();
              })}>Save text</Button>
              <Button variant="secondary" onClick={() => setEditing(null)}>Cancel</Button>
            </div>
          </div>
        ) : (
          <div className="mt-3 flex gap-2">
            <Button variant="ghost" onClick={() => { setEditing(roomId); setText(data.preview.rooms.find((r) => r.id === roomId)?.description ?? ""); }}>Edit description</Button>
            <LinkButton variant="ghost" href={`/agent/inspections/${id}/rooms/${roomId}`}>Open room</LinkButton>
          </div>
        )
      )} />
      {inReview && (
        <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur">
          <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-end gap-3 px-4 py-3">
            <span className="flex-1 text-xs text-slate-500">After finalizing, the report and PDF are locked and sent to the tenant.</span>
            <Button size="lg" disabled={!data.canFinalize} loading={busy === "finalize"} onClick={() => {
              if (confirm("Finalize the inspection? The report becomes immutable.")) void act("finalize", async () => {
                const res = await post<{ reportId: string }>(`/api/agent/inspections/${id}/finalize`);
                router.replace(`/reports/${res.reportId}`);
              });
            }}>Finalize inspection</Button>
          </div>
        </div>
      )}
    </div>
  );
}
