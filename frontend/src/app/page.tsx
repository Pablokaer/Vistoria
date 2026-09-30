"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { homeFor, useAuth } from "@/lib/auth";
import { Loading } from "@/components/ui";

export default function Home() {
  const { user, loading } = useAuth();
  const router = useRouter();
  useEffect(() => { if (!loading) router.replace(homeFor(user)); }, [loading, user, router]);
  return <div className="mx-auto max-w-md px-4"><Loading /></div>;
}
