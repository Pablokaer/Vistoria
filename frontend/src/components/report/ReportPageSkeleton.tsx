import { Skeleton } from "@/components/ui";

/** Shaped like the report page (toolbar, document, side panel) so nothing jumps when the data arrives. */
export function ReportPageSkeleton() {
  return (
    <div aria-busy="true">
      <div className="mb-5 flex items-center gap-3"><Skeleton className="h-5 w-24" /><Skeleton className="h-5 w-28" /><Skeleton className="ml-auto h-10 w-36" /></div>
      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_17rem]">
        <div className="space-y-4 rounded-lg border border-line bg-surface p-6">
          <Skeleton className="h-4 w-40" /><Skeleton className="h-7 w-2/3" /><Skeleton className="h-4 w-1/2" />
          <div className="grid grid-cols-3 gap-4 pt-4">{[0, 1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-10" />)}</div>
          <Skeleton className="h-40" />
        </div>
        <div className="hidden space-y-4 xl:block"><Skeleton className="h-48" /><Skeleton className="h-32" /></div>
      </div>
    </div>
  );
}
