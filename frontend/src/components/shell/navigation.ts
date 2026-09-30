// Navigation per role. Only destinations the role can use are listed (the API enforces access again).
import type { IconName } from "@/components/icons";
import type { ShellKey } from "@/i18n/messages/shell";
import type { Role } from "@/lib/types";

export interface NavItem { href: string; label: ShellKey; icon: IconName }
export interface NavGroup { label: ShellKey; items: NavItem[] }

const NAV: Record<Role, NavGroup[]> = {
  Company: [
    { label: "groupWorkspace", items: [
      { href: "/company", label: "navDashboard", icon: "home" },
      { href: "/company/properties", label: "navProperties", icon: "building" },
      { href: "/company/inspections/new", label: "navNewInspection", icon: "plus" },
    ] },
  ],
  Agent: [
    { label: "groupWork", items: [
      { href: "/agent", label: "navDashboard", icon: "home" },
      { href: "/agent/available", label: "navAvailable", icon: "search" },
      { href: "/agent/inspections", label: "navMyInspections", icon: "inspections" },
      { href: "/agent/completed", label: "navCompleted", icon: "checkCircle" },
    ] },
  ],
  Tenant: [
    { label: "groupWork", items: [{ href: "/tenant", label: "navMyInspections", icon: "inspections" }] },
  ],
};

const ACCOUNT: Record<Role, NavItem[]> = {
  Company: [{ href: "/company/settings", label: "navSettings", icon: "settings" }],
  Agent: [],
  Tenant: [],
};

/** A company without a workspace sees no navigation until onboarding is done. */
export function navigationFor(role: Role, hasWorkspace: boolean): { groups: NavGroup[]; account: NavItem[] } {
  if (role === "Company" && !hasWorkspace) return { groups: [], account: [] };
  return { groups: NAV[role], account: ACCOUNT[role] };
}

/** Home links match exactly; section links also match their sub-pages (e.g. /agent/inspections/123). */
export function isActive(href: string, pathname: string, role: Role): boolean {
  if (href === `/${role.toLowerCase()}`) return pathname === href;
  return pathname === href || pathname.startsWith(`${href}/`);
}
