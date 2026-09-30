"use client";

import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Icon } from "@/components/icons";
import { errorMessage, get } from "@/lib/api";
import { useT } from "@/i18n/I18nProvider";
import { authMessages } from "@/i18n/messages/auth";
import { homeFor, useAuth } from "@/lib/auth";

interface DevAccount { email: string; role: string; label: string }

/**
 * Header dropdown to jump between the seeded demo accounts. The API only maps /api/dev in Development,
 * so outside it the account list fails to load and nothing is rendered.
 */
// Visually separate from product UI: dashed, monospace "dev" chip. `placement="up"` opens above (sidebar footer).
export function DevAccountSwitcher({ placement = "down" }: { placement?: "up" | "down" }) {
  const { user, switchAccount } = useAuth();
  const t = useT(authMessages);
  const router = useRouter();
  const [accounts, setAccounts] = useState<DevAccount[] | null>(null);
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;
    get<DevAccount[]>("/api/dev/accounts").then((a) => { if (!cancelled) setAccounts(a); }, () => undefined);
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => { if (!ref.current?.contains(e.target as Node)) setOpen(false); };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  if (!accounts?.length) return null;

  async function pick(a: DevAccount) {
    setBusy(a.email);
    setError(null);
    try {
      const me = await switchAccount(a.email);
      setOpen(false);
      router.push(homeFor(me));
    } catch (e) { setError(errorMessage(e)); } finally { setBusy(null); }
  }

  return (
    <div ref={ref} className="relative">
      <button onClick={() => setOpen((o) => !o)} aria-haspopup="menu" aria-expanded={open}
        className="flex w-full items-center justify-between gap-2 whitespace-nowrap rounded-md border border-dashed border-warning-500/60 px-2 py-1 font-mono text-[11px] font-medium text-warning-500 hover:bg-warning-500/10">
        {t("devSwitch")} <Icon name="chevronDown" className="h-3.5 w-3.5" />
      </button>
      {open && (
        <div role="menu" className={`absolute z-40 w-72 rounded-lg border border-line bg-surface p-1 text-ink shadow-overlay animate-fade-in ${placement === "up" ? "bottom-full left-0 mb-1" : "right-0 mt-1"}`}>
          {accounts.map((a) => {
            const current = a.email.toLowerCase() === user?.email.toLowerCase();
            return (
              <button key={a.email} role="menuitem" disabled={current || busy !== null} onClick={() => void pick(a)}
                className={`block w-full rounded-md px-3 py-2 text-left text-sm ${current ? "bg-brand-50 text-brand" : "hover:bg-surface-2"} disabled:cursor-default`}>
                <span className="block font-medium">{a.label}{current && t("devCurrent")}{busy === a.email && " …"}</span>
                <span className="block text-caption text-ink-3">{a.email}</span>
              </button>
            );
          })}
          {error && <p className="px-3 py-2 text-caption text-danger-700">{error}</p>}
        </div>
      )}
    </div>
  );
}
