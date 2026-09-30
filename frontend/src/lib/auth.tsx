"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { api, post, refreshSession, setAccessToken, setSessionExpiredHandler, setSubscriptionRequiredHandler } from "./api";
import type { AuthResponse, Me, Role } from "./types";

interface AuthState {
  user: Me | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<Me>;
  /** Development only: sign in as one of the seeded demo accounts. */
  switchAccount: (email: string) => Promise<Me>;
  register: (data: { email: string; password: string; fullName: string; role: Role }) => Promise<Me>;
  logout: () => Promise<void>;
  reload: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export const SUBSCRIPTION_REQUIRED_PATH = "/subscription/required";

/** True when the user's role needs a subscription and the API reports no access (the API enforces it again). */
export function needsSubscription(user: Me | null): boolean {
  return !!user && user.subscription.required && !user.subscription.hasAccess;
}

/** Only same-origin relative paths are accepted as post-sign-in destinations (no open redirects). */
export function safeNext(next: string | null): string | null {
  return next && next.startsWith("/") && !next.startsWith("//") ? next : null;
}

/** Where to go after signing in: unpaid accounts always land on the subscription page, whatever `next` says. */
export function destinationAfterSignIn(user: Me, next: string | null): string {
  const target = safeNext(next);
  if (needsSubscription(user)) return target?.startsWith("/checkout") ? target : SUBSCRIPTION_REQUIRED_PATH;
  return target ?? homeFor(user);
}

export function homeFor(user: Me | null): string {
  if (!user) return "/login";
  if (needsSubscription(user)) return SUBSCRIPTION_REQUIRED_PATH;
  if (user.roles.includes("Company")) return user.company ? "/company" : "/company/onboarding";
  if (user.roles.includes("Agent")) return "/agent";
  if (user.roles.includes("Tenant")) return "/tenant";
  return "/login";
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<Me | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    setSessionExpiredHandler(() => { setUser(null); router.replace("/login?expired=1"); });
    setSubscriptionRequiredHandler(() => {
      void api<Me>("/api/auth/me").then(setUser).catch(() => undefined);
      router.replace(SUBSCRIPTION_REQUIRED_PATH);
    });
    refreshSession().then((res) => setUser(res?.user ?? null)).finally(() => setLoading(false));
  }, [router]);

  const accept = useCallback((res: AuthResponse) => { setAccessToken(res.accessToken); setUser(res.user); return res.user; }, []);

  const value = useMemo<AuthState>(() => ({
    user,
    loading,
    login: async (email, password) => accept(await post<AuthResponse>("/api/auth/login", { email, password })),
    switchAccount: async (email) => accept(await post<AuthResponse>("/api/dev/switch", { email })),
    register: async (data) => accept(await post<AuthResponse>("/api/auth/register", data)),
    logout: async () => {
      try { await post("/api/auth/logout"); } finally { setAccessToken(null); setUser(null); router.replace("/login"); }
    },
    reload: async () => setUser(await api<Me>("/api/auth/me")),
  }), [user, loading, accept, router]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
