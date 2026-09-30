// End-to-end UI flow (languages, landing + subscription, Move In + Move Out) driven through the real web app with Playwright.
// Usage: BASE_URL=http://localhost:3000 node ui-flow.mjs   (screenshots in ./output)
import { chromium, devices } from "playwright";
import { mkdirSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { abandonedCheckoutFlow, landingOnMobile, subscribeFromLanding } from "./subscription.mjs";
import { languageFlows } from "./localization.mjs";

const here = path.dirname(fileURLToPath(import.meta.url));
const BASE = process.env.BASE_URL ?? "http://localhost:3000";
const OUT = path.join(here, "output");
const ASSETS = path.join(here, "..", "backend", "src", "Infrastructure", "Seed", "Assets");
const PASSWORD = "Passw0rd!E2e";
const run = Date.now().toString(36);
mkdirSync(OUT, { recursive: true });

const log = (...a) => console.log(`[${new Date().toISOString().slice(11, 19)}]`, ...a);
let shot = 0;
const snap = async (page, name) => { await page.screenshot({ path: path.join(OUT, `${String(++shot).padStart(2, "0")}-${name}.png`), fullPage: true }); };
const asset = (name) => path.join(ASSETS, name);
const ROOM_ASSET = { "Living Room": "living-room-1.jpg", "Bedroom 1": "bedroom-1.jpg", "Bedroom 2": "bedroom-2.jpg", Kitchen: "kitchen-1.jpg", Bathroom: "bathroom-1.jpg", Garage: "garage-1.jpg" };

async function register(browser, role, email, name, opts = {}) {
  const ctx = await browser.newContext(opts.mobile ? { ...devices["Pixel 7"] } : { viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  page.on("dialog", (d) => d.accept());
  await page.goto(`${BASE}${opts.next ? `/register?role=${role}&next=${encodeURIComponent(opts.next)}` : "/register"}`);
  const label = { Company: "Company", Agent: "Inspector", Tenant: "Tenant" }[role];
  await page.getByText(label, { exact: true }).click();
  await page.getByLabel("Full name").fill(name);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(PASSWORD);
  await page.getByRole("button", { name: "Create account" }).click();
  return { ctx, page };
}

async function uploadPhoto(scope, file) {
  const before = await scope.locator("img").count();
  await scope.locator('input[type=file][multiple]').first().setInputFiles(asset(file));
  await scope.locator("img").nth(before).waitFor({ timeout: 20000 });
}

const roomSection = (page, roomName) =>
  page.locator('section[id^="room-"]', { has: page.getByRole("heading", { level: 2, name: new RegExp(`^\\d+\\. ${roomName}`) }) });

// Step 1 (one page): photos of every room and of its defects, no text.
async function captureRooms(page, rooms) {
  await page.waitForURL("**/capture");
  for (const room of rooms) {
    const section = roomSection(page, room.name);
    await section.waitFor();
    await uploadPhoto(section, ROOM_ASSET[room.name] ?? "living-room-2.jpg");
    if (room.defect) {
      await section.getByRole("button", { name: "+ Add defect" }).click();
      await uploadPhoto(section.getByTestId("defect-card").first(), room.defect.photo);
    }
  }
  await snap(page, "agent-capture");
  await page.getByRole("button", { name: "Continue to descriptions" }).click();
  await page.waitForURL("**/descriptions");
}

// Step 2 (one page): the AI texts arrive for every room; the agent reviews and completes them all.
async function describeRooms(page, rooms) {
  for (const room of rooms) {
    const section = roomSection(page, room.name);
    const description = section.getByLabel("Room description (as it will appear in the report)");
    await description.waitFor({ timeout: 60000 });
    if (room.withAi) {
      await page.waitForFunction((el) => el.value.length > 20, await description.elementHandle());
      await description.fill((await description.inputValue()).replace(/^\[Development mock AI[^\]]*\]\s*/, "") + " Inspector checked all sockets.");
    } else {
      await description.fill(`${room.name}: walls and ceiling painted white, good visible condition. Floor in good visible condition.`);
    }
    if (room.defect) {
      const card = section.getByTestId("defect-card").first();
      await card.getByLabel("Short label").fill(room.defect.label);
      await card.getByLabel("Location").fill("Wall left of the window");
      await card.getByLabel("Defect description (as it will appear in the report)").fill(room.defect.text);
      await card.getByLabel("Classification").selectOption(room.defect.classification);
      await card.getByText("I confirm this defect").click();
    }
    if (room.comparison) {
      await section.getByRole("button", { name: "Compare recorded data" }).click();
      await section.getByRole("button", { name: room.comparison.decision, exact: true }).click();
      await section.getByLabel("Comparison notes").fill(room.comparison.notes);
      await section.getByRole("button", { name: "Save decision" }).click();
      await section.getByText(/^Saved:/).waitFor();
    }
    await section.getByLabel("Agent notes").fill(`Checked ${room.name}.`);
    if (room.screenshot) await snap(page, room.screenshot);
  }
  await page.getByRole("button", { name: "Complete rooms & review" }).click();
  await page.waitForURL("**/review", { timeout: 30000 });
}

async function completeInspection(page, rooms) {
  await captureRooms(page, rooms);
  await describeRooms(page, rooms);
}

const browser = await chromium.launch(process.env.PW_CHROMIUM ? { executablePath: process.env.PW_CHROMIUM } : {});
try {
  // ---------------- Company ----------------
  const companyEmail = `company-${run}@e2e.local`;
  const tenantEmail = `tenant-${run}@e2e.local`;
  const ctxOpts = { base: BASE, snap, log };
  await languageFlows(browser, ctxOpts);
  await landingOnMobile(browser, ctxOpts);
  const { page: company } = await subscribeFromLanding(browser, { name: "Clara Company", email: companyEmail, password: PASSWORD }, ctxOpts);
  await company.getByLabel("Company name").fill(`E2E Lettings ${run}`);
  await company.getByRole("button", { name: "Create workspace" }).click();
  await company.waitForURL(/\/company$/);
  log("company workspace created");

  await company.goto(`${BASE}/company/properties/new`);
  await company.getByLabel("Address line 1").fill("12 Main Street");
  await company.getByLabel("City").fill("Dublin");
  await company.getByLabel("Postcode / Eircode").fill("D02 XY45");
  await company.getByRole("button", { name: "+ Garage" }).click();
  await snap(company, "company-create-property");
  await company.getByRole("button", { name: "Create property" }).click();
  await company.waitForURL(/\/company\/properties\/[0-9a-f-]{36}$/);
  await company.getByText("Rooms (6)").waitFor();
  const propertyUrl = company.url();
  log("property created with 6 rooms");

  await company.getByRole("button", { name: "Create tenancy" }).click();
  await company.getByRole("button", { name: "+ Invite tenant" }).click();
  await company.getByPlaceholder("Tenant name").fill("Tom Tenant");
  await company.getByPlaceholder("tenant@email.com").fill(tenantEmail);
  await company.getByRole("button", { name: "Invite", exact: true }).click();
  const inviteLink = await company.getByLabel("Tenant invitation link").inputValue();
  await snap(company, "company-property-details");
  log("tenant invited", inviteLink);

  await company.locator('main a[href*="/company/inspections/new?propertyId="]').first().click();
  await company.waitForURL("**/company/inspections/new**");
  await company.getByLabel("Tenancy").locator("option", { hasText: "Tom Tenant" }).waitFor({ state: "attached" });
  await company.getByRole("button", { name: "Create and publish" }).click();
  await company.getByText("Inspection published").waitFor();
  await company.getByRole("link", { name: "Go to inspection" }).click();
  await company.waitForURL(/\/company\/inspections\/[0-9a-f-]{36}$/);
  const moveInId = company.url().split("/").pop();
  await snap(company, "company-inspection-open");
  log("move in published", moveInId);

  // ---------------- Tenant joins ----------------
  const { page: tenant } = await register(browser, "Tenant", tenantEmail, "Tom Tenant", { next: new URL(inviteLink).pathname });
  await tenant.waitForURL("**/tenant/invite/**");
  await tenant.getByRole("button", { name: "Accept invitation" }).click();
  await tenant.waitForURL(/\/tenant$/);
  log("tenant joined tenancy");

  // ---------------- Agent (mobile) ----------------
  const { page: agent } = await register(browser, "Agent", `agent-${run}@e2e.local`, "Ian Inspector", { mobile: true });
  await agent.waitForURL(/\/agent$/);
  await agent.goto(`${BASE}/agent/available`);
  await agent.locator(`a[href="/agent/available/${moveInId}"]`).click();
  await snap(agent, "agent-available-details");
  await agent.getByRole("button", { name: "Accept inspection" }).click();
  await agent.waitForURL(`**/agent/inspections/${moveInId}`);
  await agent.getByRole("button", { name: "Start inspection" }).click();
  log("agent accepted and started");

  await completeInspection(agent, [
    { name: "Living Room", withAi: true, defect: { label: "Scuff marks", photo: "defect-scuff.jpg", text: "Light grey scuff marks approx. 30 cm wide.", classification: "PreExisting" }, screenshot: "agent-room-living-room" },
    { name: "Bedroom 1" }, { name: "Bedroom 2" }, { name: "Kitchen" }, { name: "Bathroom" }, { name: "Garage" },
  ]);
  await agent.getByText("Scuff marks").first().waitFor();
  await snap(agent, "agent-review");
  await agent.getByRole("button", { name: "Finalize inspection" }).click();
  await agent.getByRole("button", { name: "Yes, finalize" }).click(); // in-app confirmation (was window.confirm)
  await agent.waitForURL(/\/reports\/[0-9a-f-]{36}$/, { timeout: 60000 });
  const pdfHref = await agent.getByRole("link", { name: "Download PDF" }).getAttribute("href");
  const pdf = await agent.request.get(`${BASE}${pdfHref}`);
  if (!(await pdf.body()).subarray(0, 4).equals(Buffer.from("%PDF"))) throw new Error("PDF not generated");
  await snap(agent, "report-after-finalize");
  log("move in finalized; PDF ok");

  // ---------------- Tenant reviews ----------------
  await tenant.reload();
  await tenant.getByRole("link", { name: /Move In/ }).first().click();
  await tenant.waitForURL(`**/tenant/inspections/${moveInId}`);
  await tenant.getByRole("button", { name: "+ Add observation about this room" }).first().click();
  await tenant.getByPlaceholder("Your observation about this room").fill("The scuff marks were already there at viewing.");
  await tenant.getByRole("button", { name: "Add observation", exact: true }).click();
  await tenant.getByText("Tenant observation (Tom Tenant)").waitFor();
  await snap(tenant, "tenant-review");
  await tenant.getByRole("button", { name: "Everything is correct" }).click();
  // In-app confirmation dialog (replaced window.confirm in the redesign).
  await tenant.getByRole("button", { name: "Yes, the report is correct" }).click();
  await tenant.getByText("You accepted this report.").waitFor();
  log("tenant accepted move in");

  // ---------------- Move Out ----------------
  await company.goto(`${propertyUrl.replace(/\/properties\/.*/, "")}/inspections/new?propertyId=${propertyUrl.split("/").pop()}`);
  await company.getByRole("button", { name: "Move Out", exact: true }).click();
  await company.getByLabel("Compare with (Move In baseline)").locator("option").nth(1).waitFor({ state: "attached" });
  await company.getByRole("button", { name: "Create and publish" }).and(company.locator(":enabled")).waitFor();
  await company.getByRole("button", { name: "Create and publish" }).click();
  await company.getByRole("link", { name: "Go to inspection" }).click();
  await company.waitForURL(/\/company\/inspections\/[0-9a-f-]{36}$/);
  const moveOutId = company.url().split("/").pop();
  log("move out published", moveOutId);

  await agent.goto(`${BASE}/agent/available/${moveOutId}`);
  await agent.getByText("Move In report available for comparison").waitFor();
  await agent.getByRole("button", { name: "Accept inspection" }).click();
  await agent.waitForURL(`**/agent/inspections/${moveOutId}`);
  await agent.getByRole("button", { name: "Start inspection" }).click();
  await completeInspection(agent, [
    { name: "Living Room", defect: { label: "Wall stain", photo: "defect-scuff.jpg", text: "Brown stain approx. 20 cm above the sofa.", classification: "NewDamage" },
      comparison: { decision: "New damage", notes: "Stain not present at Move In." }, screenshot: "agent-move-out-room" },
    ...["Bedroom 1", "Bedroom 2", "Kitchen", "Bathroom", "Garage"].map((name) => ({ name, comparison: { decision: "Unchanged", notes: "As at Move In." } })),
  ]);
  await agent.getByText("Move In / Move Out comparison summary").waitFor();
  await agent.getByRole("button", { name: "Finalize inspection" }).click();
  await agent.getByRole("button", { name: "Yes, finalize" }).click(); // in-app confirmation (was window.confirm)
  await agent.waitForURL(/\/reports\/[0-9a-f-]{36}$/, { timeout: 60000 });
  await agent.getByText("Move In / Move Out comparison summary").waitFor();
  await snap(agent, "move-out-report");
  log("move out finalized with comparison");

  await tenant.goto(`${BASE}/tenant/inspections/${moveOutId}`);
  await tenant.getByText("Move In / Move Out comparison summary").waitFor();
  await tenant.getByRole("button", { name: "I disagree" }).click();
  await tenant.getByLabel("What do you disagree with?").fill("The stain was caused by a roof leak reported in March.");
  await tenant.getByRole("button", { name: "Dispute report" }).click();
  await tenant.getByText("You disputed this report.").waitFor();
  await snap(tenant, "tenant-dispute");

  await company.goto(`${BASE}/company`);
  await company.getByText("disputed by tenants").waitFor();
  await snap(company, "company-dashboard-final");
  await abandonedCheckoutFlow(browser, { name: "Rita Returning", email: `returning-${run}@e2e.local`, password: PASSWORD }, ctxOpts);
  log("✅ UI flow completed: subscription (direct + resumed), Move In accepted, Move Out disputed");
} catch (e) {
  for (const [i, ctx] of browser.contexts().entries())
    for (const [j, p] of ctx.pages().entries()) await p.screenshot({ path: path.join(OUT, `FAILED-${i}-${j}.png`), fullPage: true }).catch(() => {});
  throw e;
} finally {
  await browser.close();
}
