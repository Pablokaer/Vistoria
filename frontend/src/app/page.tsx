import type { Metadata } from "next";
import { AiInspectionSection } from "@/components/landing/AiInspectionSection";
import { FeatureGrid } from "@/components/landing/FeatureGrid";
import { FinalCallToAction } from "@/components/landing/FinalCallToAction";
import { ForAgentsSection } from "@/components/landing/ForAgentsSection";
import { HowItWorks } from "@/components/landing/HowItWorks";
import { LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { LandingHero } from "@/components/landing/LandingHero";
import { PricingSection } from "@/components/landing/PricingSection";
import { SignedInRedirect } from "@/components/landing/SignedInRedirect";
import { getServerLocale, getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { hasSessionHint } from "@/lib/session-hint";
import { PRODUCT_NAME } from "@/lib/brand";

export async function generateMetadata(): Promise<Metadata> {
  const [t, locale] = await Promise.all([getServerTranslator(landingMessages), getServerLocale()]);
  const title = t("metaTitle");
  const description = t("metaDescription");
  return {
    title: { absolute: title },
    description,
    openGraph: { title, description, type: "website", siteName: PRODUCT_NAME, locale: locale === "pt-BR" ? "pt_BR" : "en_GB" },
    twitter: { card: "summary", title, description },
  };
}

/**
 * "/": the public landing page for visitors; signed-in users are sent to their home instead (see SignedInRedirect).
 * Only public content is rendered here, so nothing protected can leak through this route.
 */
export default async function LandingPage() {
  const sessionHint = await hasSessionHint();
  return (
    <SignedInRedirect sessionHint={sessionHint}>
      <div className="min-h-screen bg-white text-site-ink">
        <LandingHeader />
        <main id="main">
          <LandingHero />
          <FeatureGrid />
          <AiInspectionSection />
          <HowItWorks />
          <ForAgentsSection />
          <PricingSection />
          <FinalCallToAction />
        </main>
        <LandingFooter />
      </div>
    </SignedInRedirect>
  );
}
