"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Loading } from "@/components/ui";
import { homeFor, useAuth } from "@/lib/auth";

/** Entry point of the authenticated area: sends each user to their home (login, subscription page or role dashboard). */
export default function AppEntry() {
  const { user, loading } = useAuth();
  const router = useRouter();
  useEffect(() => {
    if (loading) return;
    router.replace(user ? homeFor(user) : "/login?next=%2Fapp");
  }, [loading, user, router]);
  return <div className="mx-auto max-w-md px-4"><Loading /></div>;
}
