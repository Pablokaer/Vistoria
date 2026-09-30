"use client";

import { useEffect, useRef, useState } from "react";
import { Icon } from "@/components/icons";
import { Skeleton } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { shellMessages, type ShellKey } from "@/i18n/messages/shell";
import { get } from "@/lib/api";
import { cx } from "@/lib/cx";

// Mirrors GET /api/notifications. Subject/body are stored in English by the API, so the menu shows a title
// translated from the stable `type` instead.
interface NotificationItem { id: string; type: string; subject: string; link: string | null; createdAt: string; readAt: string | null }

const RECENT_MS = 1000 * 60 * 60 * 24 * 3;

/** Links are absolute URLs to the web app; keep only the path so navigation stays in this origin. */
function localPath(link: string | null): string | null {
  if (!link) return null;
  try { return new URL(link, window.location.origin).pathname; } catch { return null; }
}

/** Bell with a dropdown of the latest notifications. A dot marks activity in the last three days. */
export function NotificationsMenu({ tone = "light" }: { tone?: "light" | "dark" }) {
  const t = useT(shellMessages);
  const { formatDateTime } = useFormatters();
  const [items, setItems] = useState<NotificationItem[] | null>(null);
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => { get<NotificationItem[]>("/api/notifications").then(setItems, () => setItems([])); }, []);
  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => { if (!ref.current?.contains(e.target as Node)) setOpen(false); };
    const escape = (e: KeyboardEvent) => { if (e.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", escape);
    return () => { document.removeEventListener("mousedown", close); document.removeEventListener("keydown", escape); };
  }, [open]);

  // eslint-disable-next-line react-hooks/purity -- "recent" is a display hint; a stale value between renders is harmless.
  const hasRecent = !!items?.some((n) => Date.now() - new Date(n.createdAt).getTime() < RECENT_MS);
  const title = (type: string) => {
    const key = `notification.${type}` as ShellKey;
    return key in shellMessages.en ? t(key) : t("notificationFallback");
  };

  return (
    <div ref={ref} className="relative">
      <button type="button" onClick={() => setOpen((o) => !o)} aria-haspopup="true" aria-expanded={open} aria-label={t("notifications")}
        className={cx("relative grid h-9 w-9 place-items-center rounded-md transition", tone === "dark" ? "text-sidebar-ink hover:bg-white/5" : "text-ink-3 hover:bg-neutral-50 hover:text-ink")}>
        <Icon name="bell" className="h-5 w-5" />
        {hasRecent && <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-danger-500 ring-2 ring-surface" />}
      </button>
      {open && (
        <div className={cx("absolute z-40 mt-2 w-80", tone === "dark" ? "left-0" : "right-0", "max-w-[calc(100vw-2rem)] overflow-hidden rounded-lg border border-line bg-surface text-ink shadow-overlay animate-fade-in")}>
          <p className="border-b border-line px-4 py-2.5 text-label font-semibold">{t("notifications")}</p>
          <div className="max-h-96 overflow-y-auto">
            {items === null && <div className="space-y-2 p-4"><Skeleton className="h-10" /><Skeleton className="h-10" /></div>}
            {items?.length === 0 && <p className="px-4 py-6 text-label text-ink-3">{t("noNotifications")}</p>}
            {items?.map((n) => {
              const href = localPath(n.link);
              const body = (
                <>
                  <span className="block text-body font-medium text-ink">{title(n.type)}</span>
                  <span className="block text-caption text-ink-3">{formatDateTime(n.createdAt)}</span>
                </>
              );
              return href
                ? <a key={n.id} href={href} className="block border-b border-line px-4 py-2.5 last:border-0 hover:bg-surface-2">{body}</a>
                : <div key={n.id} className="border-b border-line px-4 py-2.5 last:border-0">{body}</div>;
            })}
          </div>
        </div>
      )}
    </div>
  );
}
