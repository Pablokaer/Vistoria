"use client";

import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import { useT } from "@/i18n/I18nProvider";
import { uiMessages } from "@/i18n/messages/ui";
import { Button } from "./Button";

interface ConfirmRequest { title: string; description?: ReactNode; confirmLabel?: string; danger?: boolean }

/**
 * In-app replacement for window.confirm (native <dialog>: focus trap, Esc to cancel). Returns a promise.
 * Example: `const [confirm, dialog] = useConfirm(); if (await confirm({ title: "Cancel inspection?" })) …; return <>{dialog}…</>`
 */
export function useConfirm(): [(request: ConfirmRequest) => Promise<boolean>, ReactNode] {
  const [request, setRequest] = useState<ConfirmRequest | null>(null);
  const resolver = useRef<((ok: boolean) => void) | null>(null);
  const confirm = useCallback((next: ConfirmRequest) => new Promise<boolean>((resolve) => {
    resolver.current = resolve;
    setRequest(next);
  }), []);
  const close = useCallback((ok: boolean) => {
    resolver.current?.(ok);
    resolver.current = null;
    setRequest(null);
  }, []);
  return [confirm, request ? <ConfirmDialog request={request} onClose={close} /> : null];
}

function ConfirmDialog({ request, onClose }: { request: ConfirmRequest; onClose: (ok: boolean) => void }) {
  const t = useT(uiMessages);
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => { ref.current?.showModal(); }, []);
  return (
    <dialog ref={ref} onCancel={(e) => { e.preventDefault(); onClose(false); }} aria-labelledby="confirm-title"
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-xl border border-line bg-surface p-0 text-ink shadow-overlay backdrop:bg-ink/40 animate-fade-in">
      <div className="p-5">
        <h2 id="confirm-title" className="text-section font-semibold">{request.title}</h2>
        {request.description && <div className="mt-2 text-body text-ink-3">{request.description}</div>}
      </div>
      <div className="flex justify-end gap-2 border-t border-line bg-surface-2 px-5 py-3">
        <Button variant="secondary" onClick={() => onClose(false)}>{t("cancel")}</Button>
        <Button variant={request.danger ? "danger" : "primary"} onClick={() => onClose(true)} autoFocus>{request.confirmLabel ?? t("confirm")}</Button>
      </div>
    </dialog>
  );
}
