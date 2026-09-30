"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { DevAccountSwitcher } from "@/components/DevAccountSwitcher";
import { Icon } from "@/components/icons";
import { LanguageSwitcher } from "@/components/LanguageSwitcher";
import { useT } from "@/i18n/I18nProvider";
import { shellMessages } from "@/i18n/messages/shell";
import { useAuth } from "@/lib/auth";
import { cx } from "@/lib/cx";
import type { Role } from "@/lib/types";
import { Brand } from "./Brand";
import { isActive, navigationFor, type NavItem } from "./navigation";
import { NotificationsMenu } from "./NotificationsMenu";

const ROLE_LABEL = { Company: "roleCompany", Agent: "roleAgent", Tenant: "roleTenant" } as const;

function NavLink({ item, role, onNavigate }: { item: NavItem; role: Role; onNavigate?: () => void }) {
  const t = useT(shellMessages);
  const active = isActive(item.href, usePathname(), role);
  return (
    <Link href={item.href} onClick={onNavigate} aria-current={active ? "page" : undefined}
      className={cx("group relative flex items-center gap-3 rounded-md px-3 py-2 text-body font-medium transition",
        active ? "bg-white/10 text-white" : "text-sidebar-ink hover:bg-white/5 hover:text-white")}>
      {active && <span aria-hidden="true" className="absolute inset-y-1.5 left-0 w-0.5 rounded-full bg-brand-300" />}
      <Icon name={item.icon} className={cx("h-[18px] w-[18px] shrink-0", active ? "text-brand-300" : "text-sidebar-ink/70 group-hover:text-white")} />
      {t(item.label)}
    </Link>
  );
}

/** Initials avatar — no photos are stored for users. */
export function Avatar({ name, className }: { name: string; className?: string }) {
  const initials = name.split(/\s+/).filter(Boolean).slice(0, 2).map((p) => p[0]?.toUpperCase()).join("");
  return <span aria-hidden="true" className={cx("grid h-8 w-8 shrink-0 place-items-center rounded-full bg-brand-500/25 text-caption font-semibold text-white", className)}>{initials || "?"}</span>;
}

function ProfileFooter({ role }: { role: Role }) {
  const t = useT(shellMessages);
  const { user, logout } = useAuth();
  return (
    <div className="border-t border-white/10 p-3">
      <div className="flex items-center gap-3 rounded-md px-2 py-2">
        <Avatar name={user?.fullName ?? ""} />
        <div className="min-w-0 flex-1 leading-tight">
          <p className="truncate text-label font-medium text-white">{user?.fullName}</p>
          <p className="truncate text-caption text-sidebar-ink/80">{user?.company?.companyName ?? t(ROLE_LABEL[role])}</p>
        </div>
      </div>
      <div className="mt-1 flex items-center justify-between gap-2 px-1">
        <LanguageSwitcher tone="dark" />
        <button onClick={() => void logout()} className="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-label font-medium text-sidebar-ink hover:bg-white/5 hover:text-white">
          <Icon name="logout" className="h-4 w-4" />{t("signOut")}
        </button>
      </div>
    </div>
  );
}

/**
 * Navigation rail: brand, grouped links for the role, account links, profile + language + sign out.
 * The dev account switcher sits apart at the bottom (Development only; renders nothing elsewhere).
 */
export function SidebarContent({ role, onNavigate }: { role: Role; onNavigate?: () => void }) {
  const t = useT(shellMessages);
  const { user } = useAuth();
  const { groups, account } = navigationFor(role, !!user?.company);
  return (
    <div className="flex h-full flex-col bg-sidebar">
      <div className="flex h-16 items-center justify-between gap-2 pl-5 pr-3">
        <Brand href="/app" alwaysShowName tone="dark" />
        {!onNavigate && <NotificationsMenu tone="dark" />}
      </div>
      <nav aria-label={t("mainNavigation")} className="flex-1 space-y-6 overflow-y-auto px-3 py-2">
        {groups.map((g) => (
          <div key={g.label}>
            <p className="mb-1.5 px-3 text-[11px] font-semibold uppercase tracking-wider text-sidebar-ink/60">{t(g.label)}</p>
            <div className="space-y-0.5">{g.items.map((i) => <NavLink key={i.href} item={i} role={role} onNavigate={onNavigate} />)}</div>
          </div>
        ))}
        {account.length > 0 && (
          <div>
            <p className="mb-1.5 px-3 text-[11px] font-semibold uppercase tracking-wider text-sidebar-ink/60">{t("groupAccount")}</p>
            <div className="space-y-0.5">{account.map((i) => <NavLink key={i.href} item={i} role={role} onNavigate={onNavigate} />)}</div>
          </div>
        )}
      </nav>
      <div className="px-3 pb-2"><DevAccountSwitcher placement="up" /></div>
      <ProfileFooter role={role} />
    </div>
  );
}
