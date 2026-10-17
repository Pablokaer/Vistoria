// Landing page and authentication-aware routing of "/" (ADR 0018). Used by ui-flow.mjs.
// Anonymous visitors get the landing page from the server; signed-in users are sent to their home without the
// landing page ever being rendered; logout and expired sessions bring the landing page back.
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const AXE_PATH = require.resolve("axe-core/axe.min.js");
const HERO = /Professional property/;
const PASSWORD = "Passw0rd!E2e";
const VIEWPORTS = [
  ["phone", 375, 812], ["phone-large", 430, 932], ["tablet", 768, 1024], ["laptop", 1024, 768], ["desktop", 1440, 900], ["wide", 1920, 1080],
];

const hero = (page) => page.getByRole("heading", { level: 1, name: HERO });

function fail(message) {
  throw new Error(`[landing] ${message}`);
}

/** Anonymous "/": landing from the server, no refresh call (so no 401), no console errors, working entry points. */
async function anonymousVisitor(browser, { base, snap, log }) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "en-GB" });
  const page = await ctx.newPage();
  const errors = [];
  const refreshCalls = [];
  page.on("console", (m) => { if (m.type() === "error") errors.push(m.text()); });
  page.on("request", (r) => { if (r.url().includes("/api/auth/refresh")) refreshCalls.push(r.url()); });
  const response = await page.goto(base);
  if (!HERO.test(await response.text())) fail("the landing page is not rendered on the server");
  await hero(page).waitFor();
  await page.waitForLoadState("networkidle").catch(() => undefined);
  if (refreshCalls.length > 0) fail(`anonymous visitor triggered ${refreshCalls.length} refresh call(s)`);
  if (errors.length > 0) fail(`console errors: ${errors.join(" | ")}`);
  // Product name (ADR 0020): the logo and the tab title say Olhai, never the internal code name.
  await page.getByRole("banner").getByRole("link", { name: "Olhai", exact: true }).waitFor();
  const title = await page.title();
  if (!title.includes("Olhai") || title.includes("InspectFlow")) fail(`unexpected page title: "${title}"`);
  await snap(page, "landing-desktop");

  await entryPoints(page, base);
  await page.goto(`${base}/company`); // protected route: still sends visitors to sign in
  await page.waitForURL(/\/login/);
  await ctx.close();
  log("landing: anonymous visitor OK (server-rendered, no refresh call, no console errors, entry points work)");
}

/** Every call to action leads somewhere real: sign in, company sign-up, inspector sign-in and the in-page sections. */
async function entryPoints(page, base) {
  await page.getByRole("banner").getByRole("link", { name: "Sign in" }).click();
  await page.waitForURL("**/login");
  await page.goto(base);
  await page.getByRole("banner").getByRole("link", { name: "Get started" }).click();
  await page.waitForURL("**/register?plan=professional");
  await page.goto(base);
  await page.getByRole("link", { name: "See how it works" }).click();
  await page.waitForURL("**/#how-it-works");
  await page.locator("#agents").getByRole("link", { name: "Inspector sign-in" }).click();
  await page.waitForURL("**/login");
  await page.goto(base);
  await page.getByRole("link", { name: "Get started today" }).click();
  await page.waitForURL("**/register?plan=professional");
}

/**
 * Scrolls to the bottom and back so every scroll-revealed block has risen in, then waits for the transitions.
 * Waits for hydration first: Reveal hides below-the-fold blocks when it mounts, so scrolling earlier would pass
 * them before they are watched and leave them hidden.
 */
async function revealWholePage(page) {
  await page.waitForLoadState("networkidle");
  const height = await page.evaluate(() => document.documentElement.scrollHeight);
  for (let y = 0; y < height; y += 400) {
    await page.evaluate((top) => window.scrollTo({ top, behavior: "instant" }), y);
    await page.waitForTimeout(50);
  }
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: "instant" }));
  await page.waitForFunction(() => document.querySelectorAll('[data-reveal="hidden"]').length === 0);
  await page.waitForFunction(() => document.getAnimations().every((a) => a.playState === "finished"));
}

/** No horizontal scroll and a visible hero on common viewport sizes. */
async function responsiveLayouts(browser, { base, snap, log }) {
  for (const [name, width, height] of VIEWPORTS) {
    const ctx = await browser.newContext({ viewport: { width, height }, locale: "en-GB" });
    const page = await ctx.newPage();
    await page.goto(base);
    await hero(page).waitFor();
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    if (overflow > 0) fail(`${name} (${width}px) scrolls horizontally by ${overflow}px`);
    await snap(page, `landing-${name}`);
    await ctx.close();
  }
  log("landing: layouts OK at 375 / 430 / 768 / 1024 / 1440 / 1920 px");
}

/** axe-core WCAG 2.0–2.2 A/AA rules; serious and critical violations fail the flow. */
async function accessibility(browser, { base, log }) {
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "en-GB" });
  const page = await ctx.newPage();
  await page.goto(base);
  await hero(page).waitFor();
  // Blocks below the fold rise in on scroll; contrast must be measured on the settled, fully revealed page.
  await revealWholePage(page);
  await page.addScriptTag({ path: AXE_PATH });
  const violations = await page.evaluate(async () => {
    const result = await window.axe.run(document, { runOnly: ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"] });
    return result.violations.map((v) => ({ id: v.id, impact: v.impact, nodes: v.nodes.length }));
  });
  const blocking = violations.filter((v) => v.impact === "serious" || v.impact === "critical");
  if (blocking.length > 0) fail(`accessibility violations: ${JSON.stringify(blocking)}`);
  await ctx.close();
  log(`landing: accessibility OK (axe, WCAG 2.2 AA; minor findings: ${violations.length - blocking.length})`);
}

/**
 * Signs in through the API in the browser context, as the web app does (cookies land in the context). A company,
 * the only account anyone can create without an invitation (ADR 0022, 0023); with no subscription yet, its home is
 * the subscription page.
 */
async function signInAsNewCompany(page, base, run) {
  const res = await page.request.post(`${base}/api/auth/register`, {
    headers: { "X-Client": "web" },
    data: { email: `landing-${run}@e2e.local`, password: PASSWORD, fullName: "Lena Landing", role: "Company" },
  });
  if (!res.ok()) fail(`could not register the test company: ${res.status()}`);
}

const hasCookie = async (ctx, name) => (await ctx.cookies()).some((c) => c.name === name);

// Runs before any page script: records whether the landing headline is ever attached to the DOM (a visible flash).
const WATCH_FOR_LANDING = () => {
  window.__landingSeen = false;
  new MutationObserver(() => {
    if (document.querySelector("#hero-title")) window.__landingSeen = true;
  }).observe(document, { childList: true, subtree: true });
};

/** Signed-in "/" → home without rendering the landing page; reload keeps it; logout and expiry show the landing again. */
async function signedInRouting(browser, { base, snap, log }) {
  const run = Date.now().toString(36);
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 }, locale: "en-GB" });
  const page = await ctx.newPage();
  await signInAsNewCompany(page, base, run);
  if (!(await hasCookie(ctx, "if_session"))) fail("sign-in did not set the session hint");

  await page.addInitScript(WATCH_FOR_LANDING);
  await page.goto(base);
  await page.waitForURL(/\/subscription\/required$/);
  // The landing markup travels in the RSC payload (so a stale hint can fall back without a reload) but is never rendered.
  if (await page.evaluate(() => window.__landingSeen)) fail("the landing page flashed before the redirect");
  await page.reload();
  await page.waitForURL(/\/subscription\/required$/);
  await snap(page, "landing-signed-in-redirect");

  await page.getByRole("button", { name: "Sign out" }).first().click();
  await page.waitForURL(/\/login/);
  if (await hasCookie(ctx, "if_session")) fail("logout left the session hint behind");
  await page.goto(base);
  await hero(page).waitFor();
  log("landing: signed-in redirect, reload and logout OK");

  await signInAsNewCompany(page, `${base}`, `${run}x`);
  await ctx.clearCookies({ name: "if_refresh" }); // the session ends (expired/revoked) but the hint is still there
  await page.goto(base);
  await hero(page).waitFor({ timeout: 15000 });
  if (await hasCookie(ctx, "if_session")) fail("a failed refresh left the session hint behind");
  await ctx.close();
  log("landing: expired session falls back to the landing page and clears the hint");
}

/** Runs every landing-page check. Example: `await landingFlows(browser, { base, snap, log })`. */
export async function landingFlows(browser, options) {
  await anonymousVisitor(browser, options);
  await responsiveLayouts(browser, options);
  await accessibility(browser, options);
  await signedInRouting(browser, options);
}
