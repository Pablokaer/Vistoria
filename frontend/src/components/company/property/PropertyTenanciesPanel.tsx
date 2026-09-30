"use client";

import { useState } from "react";
import { Icon } from "@/components/icons";
import { Button, Card, CopyField, EmptyState, Field, Input, StatusBadge } from "@/components/ui";
import { useFormatters, useT } from "@/i18n/I18nProvider";
import { companyPropertyMessages } from "@/i18n/messages/company-property";
import { post } from "@/lib/api";
import type { Tenancy, TenancyMember, TenantInvitation } from "@/lib/types";
import type { Run } from "./PropertyDetailsPanel";

function NewTenancyForm({ propertyId, onCreated, onCancel, run }: { propertyId: string; onCreated: () => Promise<void>; onCancel?: () => void; run: Run }) {
  const t = useT(companyPropertyMessages);
  const [form, setForm] = useState({ startDate: new Date().toISOString().slice(0, 10), endDate: "", reference: "" });
  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    void run(async () => {
      await post("/api/tenancies", { propertyId, startDate: form.startDate, endDate: form.endDate || null, reference: form.reference || null });
      await onCreated();
    });
  };
  return (
    <form className="space-y-3 border-b border-line bg-surface-2 px-4 py-4 sm:px-5" onSubmit={submit}>
      <div className="grid grid-cols-2 gap-3">
        <Field label={t("startDate")}><Input type="date" required value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></Field>
        <Field label={t("endDate")}><Input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></Field>
      </div>
      <Field label={t("reference")}><Input value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} /></Field>
      <div className="flex justify-end gap-2">
        {onCancel && <Button type="button" variant="subtle" onClick={onCancel}>{t("cancel")}</Button>}
        <Button type="submit">{t("createTenancy")}</Button>
      </div>
    </form>
  );
}

function InviteTenantForm({ tenancyId, onInvited, run }: { tenancyId: string; onInvited: (i: TenantInvitation) => Promise<void>; run: Run }) {
  const t = useT(companyPropertyMessages);
  const [invite, setInvite] = useState({ email: "", fullName: "" });
  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    void run(async () => onInvited(await post<TenantInvitation>(`/api/tenancies/${tenancyId}/tenants`, invite)));
  };
  return (
    <form className="mt-3 flex flex-wrap gap-2" onSubmit={submit}>
      <Input className="min-w-0 flex-1" aria-label={t("tenantName")} placeholder={t("tenantName")} required value={invite.fullName} onChange={(e) => setInvite({ ...invite, fullName: e.target.value })} />
      <Input className="min-w-0 flex-1" aria-label={t("tenantEmailPlaceholder")} type="email" placeholder={t("tenantEmailPlaceholder")} required value={invite.email} onChange={(e) => setInvite({ ...invite, email: e.target.value })} />
      <Button type="submit">{t("invite")}</Button>
    </form>
  );
}

function MemberRow({ member }: { member: TenancyMember }) {
  const t = useT(companyPropertyMessages);
  return (
    <li className="flex items-center gap-2 py-1.5">
      <Icon name="user" className="h-4 w-4 shrink-0 text-ink-4" />
      <span className="min-w-0 flex-1 truncate text-body"><span className="font-medium text-ink">{member.fullName}</span> <span className="text-ink-3">{member.email}</span></span>
      <span className={member.joined ? "text-caption font-medium text-success-700" : "text-caption font-medium text-warning-700"}>{member.joined ? t("joined") : t("invited")}</span>
    </li>
  );
}

function TenancyItem({ tenancy, inviting, onInvite, onInvited, run }:
  { tenancy: Tenancy; inviting: boolean; onInvite: () => void; onInvited: (i: TenantInvitation) => Promise<void>; run: Run }) {
  const t = useT(companyPropertyMessages);
  const { formatDate } = useFormatters();
  return (
    <li className="px-4 py-3 sm:px-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="flex items-center gap-2 text-body font-medium text-ink">
          <Icon name="calendar" className="h-4 w-4 text-ink-4" />
          {formatDate(tenancy.startDate)} – {tenancy.endDate ? formatDate(tenancy.endDate) : t("ongoing")}
          {tenancy.reference && <span className="font-normal text-ink-3">· {tenancy.reference}</span>}
        </span>
        <StatusBadge value={tenancy.status} />
      </div>
      <ul className="mt-1.5">
        {tenancy.members.map((m) => <MemberRow key={m.id} member={m} />)}
        {tenancy.members.length === 0 && <li className="py-1.5 text-label text-ink-3">{t("noTenants")}</li>}
      </ul>
      {inviting ? <InviteTenantForm tenancyId={tenancy.id} onInvited={onInvited} run={run} />
        : <Button variant="ghost" size="sm" className="mt-1 -ml-2" onClick={onInvite}>{t("inviteTenant")}</Button>}
    </li>
  );
}

/** Tenancies of the property with their tenants; invitation links are shown once, right after inviting. */
export function PropertyTenanciesPanel({ propertyId, tenancies, reload, run }: { propertyId: string; tenancies: Tenancy[]; reload: () => Promise<void>; run: Run }) {
  const t = useT(companyPropertyMessages);
  const [creating, setCreating] = useState(tenancies.length === 0);
  const [inviteFor, setInviteFor] = useState<string | null>(null);
  const [lastInvite, setLastInvite] = useState<TenantInvitation | null>(null);
  const invited = async (invitation: TenantInvitation) => { setLastInvite(invitation); setInviteFor(null); await reload(); };

  return (
    <Card flush title={t("tenantsTitle")} actions={!creating && <Button variant="ghost" size="sm" onClick={() => setCreating(true)}>{t("newTenancy")}</Button>}>
      {creating && <NewTenancyForm propertyId={propertyId} run={run} onCancel={tenancies.length > 0 ? () => setCreating(false) : undefined}
        onCreated={async () => { setCreating(false); await reload(); }} />}
      {lastInvite && (
        <div className="border-b border-success-500/30 bg-success-50 px-4 py-3 sm:px-5 animate-fade-in">
          <p className="mb-2 text-label text-success-700">{t("invitationCreated", { name: lastInvite.member.fullName })}</p>
          <CopyField label={t("tenantInvitationLink")} value={lastInvite.invitationLink} />
        </div>
      )}
      {tenancies.length === 0 && !creating && <div className="p-4 sm:p-5"><EmptyState icon="users" title={t("noTenancies")} /></div>}
      <ul className="divide-y divide-line">
        {tenancies.map((tn) => <TenancyItem key={tn.id} tenancy={tn} run={run} inviting={inviteFor === tn.id} onInvite={() => setInviteFor(tn.id)} onInvited={invited} />)}
      </ul>
    </Card>
  );
}
