// Public entry + subscription flows (landing → register → plan → sandbox payment → dashboard, and the
// "abandon checkout, sign in later" path). Used by ui-flow.mjs.
import { devices } from "playwright";

const desktop = { viewport: { width: 1280, height: 900 } };

async function fillAccount(page, { name, email, password }) {
  await page.getByLabel("Full name").fill(name);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Create account & continue" }).click();
}

/** On /checkout: continue to the sandbox provider and pay; ends on the post-payment destination. */
async function payInSandbox(page, { snap, declineFirst = false }) {
  await page.getByRole("heading", { name: "Choose your plan" }).waitFor();
  await page.getByTestId("plan-card").first().waitFor();
  await page.getByRole("button", { name: "Continue to payment" }).click();
  await page.waitForURL("**/checkout/sandbox/**");
  const pay = page.getByRole("button", { name: /^Pay / });
  await pay.waitFor();
  if (declineFirst) {
    await page.getByLabel("Card number").fill("4000 0000 0000 0002");
    await pay.click();
    await page.getByText("Your card was declined").waitFor();
    await page.getByLabel("Card number").fill("4242 4242 4242 4242");
  }
  await snap?.(page, "sandbox-payment");
  await pay.click();
  await page.waitForURL("**/subscription/success**");
  await page.getByRole("heading", { name: "Your subscription is active" }).waitFor({ timeout: 30000 });
}

/** Visitor → landing page → Get started → register → plan → pay → company onboarding. Returns the signed-in page. */
export async function subscribeFromLanding(browser, account, { base, snap, log }) {
  const ctx = await browser.newContext(desktop);
  const page = await ctx.newPage();
  await page.goto(base);
  await page.getByRole("heading", { level: 1, name: /Property inspections that are faster/ }).waitFor();
  for (const id of ["how-it-works", "features", "pricing"]) await page.locator(`section#${id}`).waitFor();
  await page.locator("#pricing").getByTestId("plan-card").waitFor();
  await snap(page, "landing-desktop");
  await page.getByRole("banner").getByRole("link", { name: "Get started" }).click();
  await page.waitForURL("**/register?plan=**");
  await fillAccount(page, account);
  await page.waitForURL("**/checkout?plan=**");
  await payInSandbox(page, { snap });
  await page.waitForURL("**/company/onboarding", { timeout: 15000 });
  log("company subscribed from the landing page");
  return { ctx, page };
}

/** Register → leave during checkout → sign in later → Subscription required → pay → dashboard. */
export async function abandonedCheckoutFlow(browser, account, { base, snap, log }) {
  const first = await browser.newContext(desktop);
  const page = await first.newPage();
  await page.goto(`${base}/register?plan=professional`);
  await fillAccount(page, account);
  await page.waitForURL("**/checkout?plan=**");
  await page.getByRole("button", { name: "Continue to payment" }).click();
  await page.waitForURL("**/checkout/sandbox/**");
  await first.close(); // abandoned before paying
  log("checkout abandoned");

  const later = await browser.newContext({ ...devices["Pixel 7"] });
  const phone = await later.newPage();
  await phone.goto(`${base}/company`); // a protected page sends visitors to sign in
  await phone.waitForURL("**/login?next=**");
  await phone.getByLabel("Email").fill(account.email);
  await phone.getByLabel("Password").fill(account.password);
  await phone.getByRole("button", { name: "Sign in" }).click();
  await phone.waitForURL("**/subscription/required");
  await phone.getByRole("heading", { name: "Finish setting up your subscription" }).waitFor();
  await snap(phone, "subscription-required-mobile");

  // The API refuses company features too, not just the UI.
  const blocked = await phone.evaluate(async () => {
    const auth = await (await fetch("/api/auth/refresh", { method: "POST", headers: { "X-Client": "web", "Content-Type": "application/json" }, body: "{}" })).json();
    return (await fetch("/api/company/dashboard", { headers: { Authorization: `Bearer ${auth.accessToken}` } })).status;
  });
  if (blocked !== 402) throw new Error(`Expected 402 from the API for an unpaid account, got ${blocked}`);

  await phone.getByRole("link", { name: "Continue to payment" }).click();
  await phone.waitForURL("**/checkout**");
  await payInSandbox(phone, { declineFirst: true });
  await phone.waitForURL("**/company/onboarding", { timeout: 15000 });
  await phone.getByLabel("Company name").fill(`Returning Lettings ${Date.now().toString(36)}`);
  await phone.getByRole("button", { name: "Create workspace" }).click();
  await phone.waitForURL(/\/company$/);
  await snap(phone, "company-dashboard-after-resumed-checkout");
  log("returning user resumed checkout and reached the dashboard");
  await later.close();
}

/** Landing page on a phone: the navigation collapses into a menu with the CTAs. */
export async function landingOnMobile(browser, { base, snap }) {
  const ctx = await browser.newContext({ ...devices["Pixel 7"] });
  const page = await ctx.newPage();
  await page.goto(base);
  await page.getByRole("button", { name: "Open menu" }).click();
  await page.locator("#mobile-menu").getByRole("link", { name: "Get started" }).waitFor();
  await snap(page, "landing-mobile-menu");
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  if (overflow > 1) throw new Error(`Landing page scrolls horizontally on mobile (${overflow}px)`);
  await ctx.close();
}
