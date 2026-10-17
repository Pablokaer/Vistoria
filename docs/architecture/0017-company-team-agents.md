# 0017 — Company team agents and Team inspections

**Context.** Until now an agency (company) could only publish an inspection to the public marketplace, where any independent inspector could take it, or send a private link + code to one inspector. Agencies that employ their own inspectors need two more options: offer an inspection only to their own people (whoever is free takes it) or hand it straight to one of them.

**Decision.**

- **Team membership is its own entity.** `CompanyAgent` (schema `companies`, table `company_agents`) links a company to an Agent-role user, unique per pair. It is separate from `CompanyMember` because team agents are not company staff: they get no company permissions, no company subscription requirement and keep their normal agent area. An agent may belong to several companies and still use the public marketplace.
- **Registration.** Owner/Admin (`ManageMembers`) register an agent by email. A new email creates the Agent account with the initial password the company chose (shared with the agent out of band); an email that already has an Agent account is linked as is; an email of a non-agent account is rejected. Account creation goes through `UserAccountFactory`, shared with self-registration, so both paths create the same user, role and `AgentProfile`. Any company member can read the team (`View`), because inspection creators must pick from it.
- **New visibility `Team`.** `InspectionVisibility.Team` inspections are listed (`/api/agent/available`), shown and acceptable only by agents linked to the inspection's company. The rule lives in one place — `InspectionAccess.OpenForAgent` / `CanAgentSeeAsync` — next to the existing Public and Private rules. Agents outside the team get **404**, as for private inspections, so they do not learn the inspection exists.
- **Direct assignment.** `CreateInspectionRequest.AssignedAgentId` (Team only) or `PUT /api/inspections/{id}/assignment` hand the inspection to one team agent through `Inspection.AssignToTeamAgent`. On a draft the agent is only designated, and the draft stays hidden from them. On publish, or immediately for an Open inspection, it becomes **Assigned** without an accept step. The company may switch agents, or release the inspection back to the team (`agentId: null`, **Assigned → Open**, a new transition in the state machine), until the agent starts. Changing a draft away from Team drops the designated agent.
- **Notifications.** Publishing a Team inspection notifies every team agent, or only the assigned one. Registering an agent notifies them.

**Consequences.**

- Removing an agent from the team hides future Team inspections from them. Inspections already assigned stay with the agent, so field work in progress is never taken away silently. The company can reassign the ones that have not started.
- The company chooses the initial password of a new agent account. There is no email delivery or password reset yet, so the company shares it out of band; the agent can keep it until those features exist.
- Linking an existing agent account needs no consent from the agent. Linking only lets the agent *see* the company's Team inspections. It gives the company no access to the agent's other work.
- Assigned → Open is allowed only before the agent starts (InProgress and later stay with the agent). Releasing after the acceptance deadline lets the expiry worker expire the inspection, as for any Open inspection.

**Update 2026-10-17.** Direct assignment is no longer limited to Team inspections: a Private inspection can also be sent straight to a team agent, without link + code. Release back to the team stays Team-only. See [ADR 0021](0021-private-inspection-to-team-agent.md).
