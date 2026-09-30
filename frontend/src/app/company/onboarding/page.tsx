"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Card, ErrorBanner, Field, Input, PageHeader } from "@/components/ui";
import { errorMessage, post } from "@/lib/api";
import { useAuth } from "@/lib/auth";

export default function OnboardingPage() {
  const { user, reload } = useAuth();
  const router = useRouter();
  const [name, setName] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => { if (user?.company) router.replace("/company"); }, [user, router]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await post("/api/companies", { name, contactEmail: contactEmail || null, phone: phone || null });
      await reload();
      router.replace("/company");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto max-w-xl">
      <PageHeader title="Create your company workspace" subtitle="Your properties, tenancies and inspections live in this workspace." />
      <Card>
        <ErrorBanner message={error} />
        <form onSubmit={submit} className="space-y-4">
          <Field label="Company name"><Input required value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. Dublin Lettings Ltd" /></Field>
          <Field label="Contact email (optional)" hint="Shown on reports."><Input type="email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} /></Field>
          <Field label="Phone (optional)"><Input value={phone} onChange={(e) => setPhone(e.target.value)} /></Field>
          <Button type="submit" size="lg" loading={busy}>Create workspace</Button>
        </form>
      </Card>
    </div>
  );
}
