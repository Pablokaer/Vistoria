"use client";

import { usePathname } from "next/navigation";
import { AppShell, RoleGate } from "@/components/AppShell";

export default function Layout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  return (
    <RoleGate role="Company" allowWithoutCompany={pathname === "/company/onboarding"}>
      <AppShell role="Company" wide={!pathname.includes("/rooms/")}>{children}</AppShell>
    </RoleGate>
  );
}
