"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError, errorMessage, get } from "./api";

// `errorStatus` lets pages react to a 404 without matching the (now localized) error text.
interface State<T> { key: string | null; data: T | null; error: string | null; errorStatus: number | null }

const statusOf = (e: unknown): number | null => (e instanceof ApiError ? e.status : null);

/** Fetches JSON when `path` changes (`null` skips). `reload()` refetches and resolves when done. */
export function useApi<T>(path: string | null) {
  const [state, setState] = useState<State<T>>({ key: null, data: null, error: null, errorStatus: null });
  const [reloading, setReloading] = useState(false);

  useEffect(() => {
    if (!path) return;
    let cancelled = false;
    get<T>(path).then(
      (data) => { if (!cancelled) setState({ key: path, data, error: null, errorStatus: null }); },
      (e) => { if (!cancelled) setState((s) => ({ key: path, data: s.key === path ? s.data : null, error: errorMessage(e), errorStatus: statusOf(e) })); },
    );
    return () => { cancelled = true; };
  }, [path]);

  const reload = useCallback(async () => {
    if (!path) return;
    setReloading(true);
    try {
      const data = await get<T>(path);
      setState({ key: path, data, error: null, errorStatus: null });
    } catch (e) {
      setState((s) => ({ ...s, key: path, error: errorMessage(e), errorStatus: statusOf(e) }));
    } finally {
      setReloading(false);
    }
  }, [path]);

  const setData = useCallback((data: T) => setState((s) => ({ ...s, data })), []);
  const current = state.key === path;
  return {
    data: current ? state.data : null,
    error: current ? state.error : null,
    errorStatus: current ? state.errorStatus : null,
    loading: (path !== null && !current) || reloading,
    setData,
    reload,
  };
}
