"use client";

import { usePathname } from "next/navigation";
import { AppShell, RoleGate } from "@/components/AppShell";

export default function Layout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  // Invitation links must work before the tenant has an account.
  if (pathname.startsWith("/tenant/invite/")) return <>{children}</>;
  return (
    <RoleGate role="Tenant">
      <AppShell role="Tenant">{children}</AppShell>
    </RoleGate>
  );
}
