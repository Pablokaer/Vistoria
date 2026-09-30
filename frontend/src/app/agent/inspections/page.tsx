"use client";

import { useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { MY_TABS, MyInspectionsView, type MyTab } from "@/components/agent/MyInspectionsView";
import { PageSkeleton } from "@/components/ui";

function isMyTab(value: string | null): value is MyTab {
  return !!value && (MY_TABS as string[]).includes(value) && value !== "completed";
}

function MyInspectionsFromQuery() {
  const tab = useSearchParams().get("tab");
  return <MyInspectionsView tab={isMyTab(tab) ? tab : undefined} />;
}

export default function MyInspectionsPage() {
  return <Suspense fallback={<PageSkeleton />}><MyInspectionsFromQuery /></Suspense>;
}
