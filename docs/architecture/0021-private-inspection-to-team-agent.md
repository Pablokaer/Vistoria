# 0021 — Private inspections sent straight to a team agent

**Context.** A Private inspection could only reach an inspector through a link and a 6-digit code (ADR 0017 kept direct assignment for Team inspections). Companies that pick *Private* for one of their own agents had to copy the link and the code, send them, and wait for the agent to type the code and accept. They asked for the agent list in the Private option, so the inspection goes straight into the chosen agent's account.

**Decision.**

- **Private accepts a direct assignment.** `Inspection.AssignToTeamAgent` now accepts Team **and** Private inspections; only Public ones are refused (`inspection.public_not_assignable`). Every other rule from ADR 0017 applies unchanged: the agent must be registered in the company's team (`CompanyAgent`), a draft only designates the agent, publishing makes it **Assigned** without an accept step, and the company can switch agents until the agent starts. `CreateInspectionRequest.AssignedAgentId` and `PUT /api/inspections/{id}/assignment` work for both visibilities.
- **No invitation when an agent is chosen.** Publishing a Private inspection that already has an agent creates no link + code (`PublishResult.invitation` is `null`), and `inviteEmail` is ignored. The agent is notified as for Team (`team.inspection_assigned`).
- **Earlier invitations stop working.** Assigning an Open Private inspection to a team agent revokes its outstanding invitations, so an outside inspector holding the link can no longer open or accept it.
- **No release.** A Private inspection has no team list to go back to. `ReleaseToTeam` stays Team-only (`inspection.not_team`), and the API offers `assign` but never `release` for Private inspections. To go back to an outside inspector, the company cancels the inspection and creates a new one.
- **Drafts keep the agent between Team and Private.** Editing a draft to Public drops the designated agent; switching between Team and Private keeps it.
- **UI.** In the inspection form the Private option shows a *Send to* dropdown with the team's agents and *An outside inspector (link + 6-digit code)*. The invitation email field appears only for the outside inspector. The same `TeamAgentPicker` serves Team (`mode="team"`, empty choice = whole team) and Private (`mode="private"`, empty choice = outside inspector).

**Consequences.**

- The difference between Team and Private now lies in who may see the inspection *before* it is assigned: with Team, the whole team sees it; with Private, nobody except the invited outside inspector sees it. When an agent is chosen, both behave the same, but the inspection keeps the visibility the company picked.
- Other agents of the team never see a Private inspection, even when it is assigned to a colleague.
- The audit log records the assignment (`InspectionAssignedToAgent`) for both visibilities.

**Superseded 2026-10-17** by [ADR 0022](0022-agents-belong-to-their-company.md): Private inspections and the link + 6-digit code were removed. Sending an inspection straight to one agent is now the `Direct` visibility.
