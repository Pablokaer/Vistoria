// Language flows: the browser language picks the first language, the header switcher changes it and the choice
// survives a reload, and API errors come back in the chosen language. Used by ui-flow.mjs.

const desktop = { viewport: { width: 1280, height: 900 } };
const PT_HERO = /Vistorias de imóveis mais rápidas/;
const EN_HERO = /Property inspections that are faster/;

async function expectLang(page, lang) {
  const actual = await page.locator("html").getAttribute("lang");
  if (actual !== lang) throw new Error(`Expected <html lang="${lang}">, got "${actual}" on ${page.url()}`);
}

/** A Portuguese browser gets the landing page and sign-in in Portuguese, with a Portuguese API error. */
async function portugueseBrowser(browser, { base, snap, log }) {
  const ctx = await browser.newContext({ ...desktop, locale: "pt-BR" });
  const page = await ctx.newPage();
  await page.goto(base);
  await page.getByRole("heading", { level: 1, name: PT_HERO }).waitFor();
  await expectLang(page, "pt-BR");
  await snap?.(page, "landing-pt-BR");
  await page.goto(`${base}/login`);
  await page.getByLabel("E-mail").fill(`nobody-${Date.now()}@e2e.local`);
  await page.getByLabel("Senha").fill("WrongPass1!");
  await page.getByRole("button", { name: "Entrar" }).click();
  // The API answers 401 in the Accept-Language the web app sends; no English text may leak through.
  // Next.js adds an empty role="alert" route announcer; the error banner is the one with text.
  const alert = page.getByRole("alert").filter({ hasText: /\S/ }).first();
  await alert.waitFor();
  const text = (await alert.innerText()).trim();
  if (!text || /invalid|incorrect|failed/i.test(text)) throw new Error(`Login error is empty or still English: "${text}"`);
  log(`pt-BR login error: "${text}"`);
  await ctx.close();
}

/** An English browser switches to Portuguese in the header; the cookie keeps it after a reload. */
async function switchLanguage(browser, { base, snap }) {
  const ctx = await browser.newContext({ ...desktop, locale: "en-GB" });
  const page = await ctx.newPage();
  await page.goto(base);
  await page.getByRole("heading", { level: 1, name: EN_HERO }).waitFor();
  await page.getByTestId("language-switcher").first().selectOption("pt-BR");
  await page.getByRole("heading", { level: 1, name: PT_HERO }).waitFor();
  await page.reload();
  await page.getByRole("heading", { level: 1, name: PT_HERO }).waitFor();
  await expectLang(page, "pt-BR");
  await snap?.(page, "landing-switched-pt-BR");
  await page.getByTestId("language-switcher").first().selectOption("en");
  await page.getByRole("heading", { level: 1, name: EN_HERO }).waitFor();
  await ctx.close();
}

/** Runs both language flows. Example: `await languageFlows(browser, { base, snap, log })`. */
export async function languageFlows(browser, options) {
  await portugueseBrowser(browser, options);
  await switchLanguage(browser, options);
  options.log("Language flows OK (browser language, switcher, cookie, API errors)");
}
