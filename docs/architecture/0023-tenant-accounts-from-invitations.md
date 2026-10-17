# 0023 — Tenant accounts are created only from their invitation

**Context.** The platform does not send email yet, so it cannot confirm that whoever signs up owns the address they typed. Anyone could register a tenant account with someone else's address before the real tenant did. That squatted the address: the real tenant could no longer sign up, and the account would match the email check when a tenancy invitation for that address was accepted. The tenant invitation is the one thing a tenant always has: the company creates it, and its link carries a 256-bit token that is single use and expires. ADR 0022 already removed agent sign-up, so companies were the only other self-registrable accounts.

**Decision.**

- **Tenant sign-up needs the invitation.** `POST /api/auth/register` with `role: Tenant` requires `invitationToken`:
  - a missing token returns 400 (`InvitationToken`, "Tenant accounts are created from the invitation link…");
  - an unknown, used or expired token returns 404;
  - an email other than the invited one returns 403, with the same text as accepting an invitation.

  The account is created and joins that tenancy in one transaction, which also spends the token. Two sign-ups from the same link race on the invitation row's version, and the loser gets 409.
- **Further invitations** for a tenant who already has an account still go through `POST /api/tenant/invitations/{token}/accept` while signed in, again with the email check.
- **One place for invitation rules.** `TenancyInvitationLookup` (Tenancies module) finds open invitations by token, checks the invited email and joins the tenancy, recording `TenantJoinedTenancy`. `AuthService.RegisterAsync` and `TenancyService` (preview, accept) both use it.
- **Web.**
  - The invitation page's *Create account* opens `/register?invite=<token>`: a tenant form with no role choice, a hint to use the invited address, and *Sign in* returning to the invitation.
  - Plain `/register` is the company sign-up. It explains that tenants use their invitation link and inspectors get their account from their company.
- **Demo data.** The demo tenant signs up from the first demo tenancy's invitation and accepts the second one.

**Consequences.**

- A tenant address can only be claimed by someone who received that address's invitation link.
- Company accounts are still created without email verification. A company account could squat a tenant's address, but it cannot join a tenancy because the role is wrong. Real verification needs email delivery, which is not built yet; it is listed as a known limitation.
- The invited email check is the same for sign-up and for acceptance, so a forwarded link still cannot be used with another address.
