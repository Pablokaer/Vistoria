// Team agents (ADR 0022): the company creates its agents' accounts, sends a Direct inspection straight to one of them,
// and that agent finds it under My inspections (no accept step) while a colleague never sees it. Used by ui-flow.mjs.

/**
 * Company page: /company/agents → create a new agent account (agents cannot sign up themselves).
 * Example: `await registerTeamAgent(companyPage, { name, email, password }, ctxOpts)`.
 */
export async function registerTeamAgent(company, agent, { base, snap, log }) {
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

/** Company page: new Periodic inspection, Direct to the agent. Returns the inspection id. */
async function publishDirectInspection(company, propertyId, agent, { base, snap, log }) {
  await company.goto(`${base}/company/inspections/new?propertyId=${propertyId}`);
  await company.getByRole("button", { name: "Periodic", exact: true }).click();
  await company.getByText("You choose the agent; only they see it.").click();
  const picker = company.getByTestId("direct-agent");
  await picker.locator("option", { hasText: agent.name }).waitFor({ state: "attached" });
  await picker.selectOption({ label: `${agent.name} · ${agent.email}` });
  await company.getByRole("button", { name: "Create and publish" }).click();
  await company.getByText(`${agent.name} was assigned and notified.`, { exact: false }).waitFor();
  await snap(company, "company-team-inspection-assigned");
  await company.getByRole("link", { name: "Go to inspection" }).click();
  await company.waitForURL(/\/company\/inspections\/[0-9a-f-]{36}$/);
  await company.getByText("Team assignment").waitFor();
  log("direct inspection published and assigned");
  return company.url().split("/").pop();
}

/** A colleague's Available list must not show an inspection sent directly to someone else. */
async function colleagueDoesNotSeeIt(colleague, inspectionId, { base, log }) {
  await colleague.goto(`${base}/agent/available`);
  await colleague.getByRole("heading", { name: "Available inspections" }).waitFor();
  if (await colleague.locator(`a[href="/agent/available/${inspectionId}"]`).count())
    throw new Error(`direct inspection ${inspectionId} must not be listed to a colleague`);
  log("colleague does not see the direct inspection");
}

/** A fresh browser session signs in as the agent, checks the assigned inspection is listed and opens it. */
async function agentSeesAssignment(browser, agent, inspectionId, { base, snap, log }) {
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  await page.goto(`${base}/login`);
  await page.getByLabel("Email").fill(agent.email);
  await page.getByLabel("Password", { exact: true }).fill(agent.password);
  await page.getByRole("button", { name: "Sign in" }).click();
  await page.waitForURL(/\/agent$/);
  await page.goto(`${base}/agent/inspections`);
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
  log("team agent sees the direct inspection; sidebar PT | EN switch OK");
  await ctx.close();
}

/**
 * Full team scenario: a second agent gets a Direct inspection; `colleague` (an agent page already signed in) must not
 * see it. Example: `await teamFlow(browser, companyPage, propertyId, { name, email, password }, agentPage, ctxOpts)`.
 */
export async function teamFlow(browser, company, propertyId, agent, colleague, ctxOpts) {
  await registerTeamAgent(company, agent, ctxOpts);
  const directInspectionId = await publishDirectInspection(company, propertyId, agent, ctxOpts);
  await colleagueDoesNotSeeIt(colleague, directInspectionId, ctxOpts);
  await agentSeesAssignment(browser, agent, directInspectionId, ctxOpts);
}
