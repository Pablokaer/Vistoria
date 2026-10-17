// Team agents: the company registers its own agent, publishes a Team inspection assigned to them and a Private one
// sent straight to them (no link + code), and the agent finds both under My inspections (no accept step). Used by ui-flow.mjs.

/** Company page: /company/agents → register a new agent account. */
async function registerTeamAgent(company, agent, { base, snap, log }) {
  await company.goto(`${base}/company/agents`);
  const form = company.getByTestId("register-agent");
  await form.getByLabel("Full name").fill(agent.name);
  await form.getByLabel("Email").fill(agent.email);
  await form.getByLabel("Initial password").fill(agent.password);
  await form.getByRole("button", { name: "Register agent" }).click();
  await company.getByTestId("team-agent-row").filter({ hasText: agent.email }).waitFor();
  await snap(company, "company-team-agents");
  log("team agent registered", agent.email);
}

/** Company page: new Periodic inspection, visibility Team, assigned to the agent. Returns the inspection id. */
async function publishAssignedInspection(company, propertyId, agent, { base, snap, log }) {
  await company.goto(`${base}/company/inspections/new?propertyId=${propertyId}`);
  await company.getByRole("button", { name: "Periodic", exact: true }).click();
  await company.getByText("Only the agents registered in your team.").click();
  const picker = company.getByTestId("team-agent");
  await picker.locator("option", { hasText: agent.name }).waitFor({ state: "attached" });
  await picker.selectOption({ label: `${agent.name} · ${agent.email}` });
  await company.getByRole("button", { name: "Create and publish" }).click();
  await company.getByText(`${agent.name} was assigned and notified.`, { exact: false }).waitFor();
  await snap(company, "company-team-inspection-assigned");
  await company.getByRole("link", { name: "Go to inspection" }).click();
  await company.waitForURL(/\/company\/inspections\/[0-9a-f-]{36}$/);
  await company.getByText("Team assignment").waitFor();
  log("team inspection published and assigned");
  return company.url().split("/").pop();
}

/** Company page: new Periodic inspection, visibility Private, sent straight to the agent. Returns the inspection id. */
async function publishPrivateToAgent(company, propertyId, agent, { base, snap, log }) {
  await company.goto(`${base}/company/inspections/new?propertyId=${propertyId}`);
  await company.getByRole("button", { name: "Periodic", exact: true }).click();
  await company.getByText("One agent of your team, or an outside agent with the link and the 6-digit code.").click();
  const picker = company.getByTestId("private-agent");
  await picker.locator("option", { hasText: agent.name }).waitFor({ state: "attached" });
  await picker.selectOption({ label: `${agent.name} · ${agent.email}` });
  if (await company.getByLabel("Agent email (optional)").count()) throw new Error("a Private inspection sent to a team agent must not ask for an invitation email");
  await company.getByRole("button", { name: "Create and publish" }).click();
  await company.getByText(`${agent.name} was assigned and notified.`, { exact: false }).waitFor();
  if (await company.getByText("Access code").count()) throw new Error("a Private inspection sent to a team agent must not show a link + code");
  await snap(company, "company-private-inspection-sent-to-agent");
  await company.getByRole("link", { name: "Go to inspection" }).click();
  await company.waitForURL(/\/company\/inspections\/[0-9a-f-]{36}$/);
  log("private inspection sent straight to the team agent");
  return company.url().split("/").pop();
}

/** A fresh browser session signs in as the agent, checks every assigned inspection is listed and opens the first. */
async function agentSeesAssignment(browser, agent, [inspectionId, ...otherIds], { base, snap, log }) {
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(`${base}/login`);
  await page.getByLabel("Email").fill(agent.email);
  await page.getByLabel("Password", { exact: true }).fill(agent.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL(/\/agent$/);
  await page.goto(`${base}/agent/inspections`);
  for (const id of otherIds) await page.locator(`a[href="/agent/inspections/${id}"]`).first().waitFor();
  await page.locator(`a[href="/agent/inspections/${inspectionId}"]`).first().click();
  await page.getByRole("button", { name: "Start inspection" }).waitFor();
  await snap(page, "team-agent-assigned-inspection");
  // Sidebar PT | EN switch: one click changes the whole app.
  const toggle = page.getByTestId("sidebar-language-toggle");
  await toggle.getByRole("radio", { name: "PT" }).click();
  await page.getByRole("button", { name: "Iniciar vistoria" }).waitFor();
  await snap(page, "sidebar-language-pt");
  await toggle.getByRole("radio", { name: "EN" }).click();
  await page.getByRole("button", { name: "Start inspection" }).waitFor();
  log("team agent sees the assigned inspections; sidebar PT | EN switch OK");
  await ctx.close();
}

/**
 * Full team scenario. Example: `await teamFlow(browser, companyPage, propertyId, { name, email, password }, ctxOpts)`.
 */
export async function teamFlow(browser, company, propertyId, agent, ctxOpts) {
  await registerTeamAgent(company, agent, ctxOpts);
  const teamInspectionId = await publishAssignedInspection(company, propertyId, agent, ctxOpts);
  const privateInspectionId = await publishPrivateToAgent(company, propertyId, agent, ctxOpts);
  await agentSeesAssignment(browser, agent, [teamInspectionId, privateInspectionId], ctxOpts);
}
