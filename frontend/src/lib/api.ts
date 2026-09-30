// Minimal API client. The access token lives in memory only; the refresh token is an HttpOnly cookie
// (set by the API because we send X-Client: web). All calls are same-origin via the Next.js rewrite.

import type { AuthResponse } from "./types";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public code?: string,
    public details: string[] = [],
    public errors: Record<string, string[]> = {},
  ) {
    super(message);
  }
}

let accessToken: string | null = null;
let refreshing: Promise<AuthResponse | null> | null = null;
let onSessionExpired: (() => void) | null = null;

export function setAccessToken(token: string | null) { accessToken = token; }
export function setSessionExpiredHandler(handler: () => void) { onSessionExpired = handler; }

async function parseError(res: Response): Promise<ApiError> {
  let body: { title?: string; code?: string; details?: string[]; errors?: Record<string, string[]> } = {};
  try { body = await res.json(); } catch { /* not JSON */ }
  const fallback = res.status === 429 ? "Too many requests. Please wait a moment." : `Request failed (${res.status})`;
  return new ApiError(res.status, body.title ?? fallback, body.code, body.details ?? [], body.errors ?? {});
}

/** Uses the HttpOnly refresh cookie. Concurrent callers share one refresh request. */
export function refreshSession(): Promise<AuthResponse | null> {
  if (!refreshing) {
    refreshing = fetch("/api/auth/refresh", { method: "POST", headers: { "X-Client": "web", "Content-Type": "application/json" }, body: "{}", credentials: "same-origin" })
      .then(async (res) => {
        if (!res.ok) { accessToken = null; return null; }
        const data = (await res.json()) as AuthResponse;
        accessToken = data.accessToken;
        return data;
      })
      .catch(() => null)
      .finally(() => { refreshing = null; });
  }
  return refreshing;
}

export async function api<T>(path: string, init: RequestInit & { json?: unknown } = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("X-Client", "web");
  if (accessToken) headers.set("Authorization", `Bearer ${accessToken}`);
  let body = init.body;
  if (init.json !== undefined) {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(init.json);
  }
  const res = await fetch(path, { ...init, headers, body, credentials: "same-origin" });
  if (res.status === 401 && retry && !path.startsWith("/api/auth/")) {
    const refreshed = await refreshSession();
    if (refreshed) return api<T>(path, init, false);
    onSessionExpired?.();
  }
  if (!res.ok) throw await parseError(res);
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const get = <T,>(path: string) => api<T>(path);
export const post = <T,>(path: string, json?: unknown) => api<T>(path, { method: "POST", json: json ?? {} });
export const put = <T,>(path: string, json: unknown) => api<T>(path, { method: "PUT", json });
export const del = <T,>(path: string) => api<T>(path, { method: "DELETE" });

/** Multipart upload with progress (fetch has no upload progress events). */
export function uploadWithProgress<T>(path: string, form: FormData, onProgress?: (fraction: number) => void): Promise<T> {
  const send = (token: string | null) => new Promise<{ status: number; body: string }>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", path);
    xhr.setRequestHeader("X-Client", "web");
    if (token) xhr.setRequestHeader("Authorization", `Bearer ${token}`);
    xhr.upload.onprogress = (e) => { if (e.lengthComputable) onProgress?.(e.loaded / e.total); };
    xhr.onload = () => resolve({ status: xhr.status, body: xhr.responseText });
    xhr.onerror = () => reject(new ApiError(0, "Network error — check your connection and retry."));
    xhr.send(form);
  });
  return (async () => {
    let result = await send(accessToken);
    if (result.status === 401 && (await refreshSession())) result = await send(accessToken);
    if (result.status < 200 || result.status >= 300) {
      let parsed: { title?: string; code?: string; details?: string[]; errors?: Record<string, string[]> } = {};
      try { parsed = JSON.parse(result.body); } catch { /* ignore */ }
      const firstError = parsed.errors ? Object.values(parsed.errors)[0]?.[0] : undefined;
      throw new ApiError(result.status, firstError ?? parsed.title ?? `Upload failed (${result.status})`, parsed.code, parsed.details ?? [], parsed.errors ?? {});
    }
    return JSON.parse(result.body) as T;
  })();
}

export function errorMessage(e: unknown): string {
  if (e instanceof ApiError) {
    const fieldErrors = Object.values(e.errors).flat();
    return fieldErrors.length > 0 ? fieldErrors.join(" ") : e.message;
  }
  return e instanceof Error ? e.message : "Something went wrong.";
}
