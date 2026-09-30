"use client";

import { MyInspectionsView } from "@/components/agent/MyInspectionsView";

/** Kept as its own route (bookmarks, navigation) — the same tabbed view with Completed selected. */
export default function CompletedPage() {
  return <MyInspectionsView tab="completed" />;
}
