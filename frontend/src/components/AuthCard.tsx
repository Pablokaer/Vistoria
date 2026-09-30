"use client";

import { Brand } from "./AppShell";
import { Icon, type IconName } from "./icons";
import { LanguageSwitcher } from "./LanguageSwitcher";
import { useT } from "@/i18n/I18nProvider";
import { authMessages } from "@/i18n/messages/auth";

const POINTS: { icon: IconName; key: "panelPoint1" | "panelPoint2" | "panelPoint3" }[] = [
  { icon: "rooms", key: "panelPoint1" },
  { icon: "sparkles", key: "panelPoint2" },
  { icon: "shield", key: "panelPoint3" },
];

/** Navy product panel beside the form on large screens: what InspectFlow is, in three real capabilities. */
function ProductPanel() {
  const t = useT(authMessages);
  return (
    <aside className="relative hidden overflow-hidden bg-sidebar p-10 text-white lg:flex lg:flex-col lg:justify-between">
      <Brand alwaysShowName tone="dark" />
      <div className="max-w-sm">
        <p className="text-[1.75rem] font-semibold leading-tight tracking-tight">{t("panelTitle")}</p>
        <ul className="mt-8 space-y-4">
          {POINTS.map((p) => (
            <li key={p.key} className="flex gap-3 text-body text-sidebar-ink">
              <span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-white/10 text-brand-300"><Icon name={p.icon} className="h-4.5 w-4.5" /></span>
              <span className="pt-1.5">{t(p.key)}</span>
            </li>
          ))}
        </ul>
      </div>
      {/* Blueprint grid: a quiet nod to floor plans, not decoration for its own sake. */}
      <div aria-hidden="true" className="pointer-events-none absolute inset-0 opacity-[0.06] [background-image:linear-gradient(white_1px,transparent_1px),linear-gradient(90deg,white_1px,transparent_1px)] [background-size:32px_32px]" />
      <p className="relative text-caption text-sidebar-ink/70">© InspectFlow</p>
    </aside>
  );
}

/** Sign-in / sign-up frame; the language can be changed here, before the user has an account. */
export function AuthCard({ title, subtitle, children }: { title: string; subtitle?: string; children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <ProductPanel />
      <div className="flex flex-col px-4 py-6 sm:px-8">
        <div className="flex items-center justify-between lg:justify-end"><span className="lg:hidden"><Brand alwaysShowName /></span><LanguageSwitcher /></div>
        <div className="flex flex-1 items-start justify-center py-8 sm:items-center">
          <div className="w-full max-w-md">
            <h1 className="text-page font-semibold tracking-tight text-ink">{title}</h1>
            {subtitle && <p className="mt-1.5 text-body text-ink-3">{subtitle}</p>}
            <div className="mt-6 rounded-lg border border-line bg-surface p-5 shadow-card sm:p-6">{children}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
