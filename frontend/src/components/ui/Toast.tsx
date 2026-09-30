"use client";

import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";
import { Icon, type IconName } from "@/components/icons";
import { useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";
import { cx } from "@/lib/cx";

export type ToastTone = "success" | "info" | "error" | "ai";
interface ToastItem { id: number; tone: ToastTone; title: string; description?: string }
interface ToastApi { notify: (toast: Omit<ToastItem, "id">) => void }

const ToastContext = createContext<ToastApi | null>(null);

const TONES: Record<ToastTone, { icon: IconName; className: string }> = {
  success: { icon: "checkCircle", className: "text-success-500" },
  info: { icon: "info", className: "text-info-500" },
  error: { icon: "alert", className: "text-danger-500" },
  ai: { icon: "sparkles", className: "text-review-500" },
};

const VISIBLE_MS = 4000;

/** Renders transient feedback (saved, uploaded, accepted, finalized…) in a polite live region. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);
  const nextId = useRef(1);
  const dismiss = useCallback((id: number) => setItems((all) => all.filter((t) => t.id !== id)), []);
  const notify = useCallback((toast: Omit<ToastItem, "id">) => {
    const id = nextId.current++;
    setItems((all) => [...all.slice(-2), { ...toast, id }]);
    window.setTimeout(() => dismiss(id), VISIBLE_MS);
  }, [dismiss]);
  const api = useMemo(() => ({ notify }), [notify]);
  return (
    <ToastContext.Provider value={api}>
      {children}
      <ToastViewport items={items} onDismiss={dismiss} />
    </ToastContext.Provider>
  );
}

function ToastViewport({ items, onDismiss }: { items: ToastItem[]; onDismiss: (id: number) => void }) {
  const t = useT(uiMessages);
  return (
    <div aria-live="polite" className="pointer-events-none fixed inset-x-0 top-16 z-50 flex flex-col items-center gap-2 px-4 lg:inset-x-auto lg:bottom-6 lg:right-6 lg:top-auto lg:items-end">
      {items.map((item) => (
        <div key={item.id} role="status" className="pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-lg border border-line bg-surface px-4 py-3 shadow-overlay animate-fade-in">
          <Icon name={TONES[item.tone].icon} className={cx("mt-0.5 h-5 w-5 shrink-0", TONES[item.tone].className)} />
          <div className="min-w-0 flex-1">
            <p className="text-body font-medium text-ink">{item.title}</p>
            {item.description && <p className="mt-0.5 text-label text-ink-3">{item.description}</p>}
          </div>
          <button onClick={() => onDismiss(item.id)} aria-label={t("dismiss")} className="text-ink-4 hover:text-ink-2"><Icon name="close" className="h-4 w-4" /></button>
        </div>
      ))}
    </div>
  );
}

/** Example: `const toast = useToast(); toast.notify({ tone: "success", title: t("saved") });` */
export function useToast(): ToastApi {
  const api = useContext(ToastContext);
  if (!api) throw new Error("useToast() was called outside <ToastProvider>; it is mounted in app/layout.tsx.");
  return api;
}
