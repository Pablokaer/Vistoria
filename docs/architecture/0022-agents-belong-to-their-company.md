# 0022 — Agents belong to their company: Public (team) or Direct inspections, no marketplace

**Context.** A security review showed that the open marketplace let anyone abuse the platform. Agent self-sign-up was free and did not verify the email. Accepting a Public inspection had no limit and revealed the full address and the tenants' names. The company could not take an accepted Public inspection back, only cancel it. An assigned agent could also request AI analyses at 30 per minute, which costs the company money. Companies could also add an existing agent account to their team just by knowing its email, without the agent agreeing, and then see the agent's phone number. The product owner defined the intended model: **companies create their agents' accounts, and an agent only sees the inspections its company published as public, or the ones sent directly to it when the inspection was created.**

**Decision.**

- **No agent self-sign-up.** `AppRoles.IsSelfRegistrable` accepts only `Company` and `Tenant`; `POST /api/auth/register` with `Agent` returns 400 ("agent accounts are created by their company"). The web register page has no Inspector option and explains where inspectors sign in; the landing page's inspector call to action is *Inspector sign-in* (`/login`).
- **The company always creates the account.** `POST /api/companies/me/agents` creates a new Agent account (password required) and adds it to the team. An email that already has **any** account is refused with the same message whatever the role or company, so the role of an address is not revealed and no existing account can be taken over. An agent therefore belongs to the company that created it. Team links made before this change (`CompanyAgent`) stay.
- **Two visibilities instead of three.** `InspectionVisibility` is now `Public` or `Direct`:
  - **Public** — listed under *Available* only to the agents of the publishing company (`InspectionAccess.OpenForAgent` and `CanAgentSeeAsync` both require a `CompanyAgent` link). The first to accept takes it. An agent of another company gets 404 on the list item, the details and the accept endpoint.
  - **Direct** — the company chose the agent (`assignedAgentId` at creation, or `PUT /api/inspections/{id}/assignment` later). A draft only designates the agent; publishing makes it **Assigned** without an accept step. Colleagues never see it.
  - The visibility is derived, not sent: `CreateInspectionRequest` and `UpdateDraftRequest` no longer have a `visibility` field. Assigning makes an inspection Direct, and releasing makes it Public again.
- **The company can take an inspection back until the agent starts.** Assignment and release work for any inspection that is Draft, Open or Assigned, including a Public one an agent accepted from the list (`release` is offered whenever an agent holds it). Releasing an inspection without an agent returns `inspection.not_assigned`. Once the agent starts, both are refused, as before.
- **Private invitations are removed.** The link plus 6-digit code for outside inspectors no longer fits a model without outside inspectors. Removed:
  - the `InspectionInvitation` entity and its table (migration `AgentsBelongToTheirCompany`);
  - `InvitationService` and the access-code hasher;
  - `GET/POST /api/agent/invitations/{token}[/verify]` and `POST /api/inspections/{id}/invitation`;
  - the invitation status in the inspection details, the invite page `/inspection/invite/{token}`, and the invitation-related audit actions and notification type.

  The publish endpoint takes no body, and `POST /api/inspections` and `POST /api/inspections/{id}/publish` return the inspection details directly, with no `{ inspection, invitation }` wrapper.
- **Data migration.** Team or Private inspections that already have an agent become Direct. All other Team or Private inspections become Public. An open Private inspection that was waiting for an outside inspector's code is therefore now listed to the company's own agents instead. Inspections that outside agents accepted earlier stay with those agents until they finish. `Down` maps Direct back to Team but does not restore invitations.
- **Demo data.** The demo company creates the demo agent through `CompanyAgentService`. Databases seeded earlier keep their link (`EnsureDemoTeamAgentAsync`).

**Consequences.**

- An agent can only reach the addresses and tenant names of its own company's inspections. A stranger can no longer sign up, grab Public inspections or spend the company's AI budget.
- Inspectors who worked for several agencies need one account (email) per agency.
- Removing an agent from the team hides the company's future Public inspections from them. Inspections already assigned stay with the agent, as in ADR 0017.
- This supersedes the marketplace part of ADR 0017 and all of ADR 0021.
