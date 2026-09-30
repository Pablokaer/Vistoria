"use client";

import { usePathname } from "next/navigation";
import { AppShell, RoleGate } from "@/components/AppShell";

export default function Layout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  return (
    <RoleGate role="Agent">
      <AppShell role="Agent" wide={!pathname.includes("/rooms/")}>{children}</AppShell>
    </RoleGate>
  );
}
