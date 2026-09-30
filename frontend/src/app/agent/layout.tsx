"use client";

import { AppShell, RoleGate } from "@/components/AppShell";

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <RoleGate role="Agent">
      <AppShell role="Agent" wide>{children}</AppShell>
    </RoleGate>
  );
}
