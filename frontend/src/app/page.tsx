import type { Metadata } from "next";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { AudienceSection } from "@/components/landing/AudienceSection";
import { FeaturesSection } from "@/components/landing/FeaturesSection";
import { HeroSection } from "@/components/landing/HeroSection";
import { HighlightsSection } from "@/components/landing/HighlightsSection";
import { HowItWorksSection } from "@/components/landing/HowItWorksSection";
import { FinalCallToAction, LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { PricingSection } from "@/components/landing/PricingSection";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getServerTranslator(landingMessages);
  return { title: { absolute: t("metaTitle") } };
}

/** Public landing page. Signed-in users reach their area through "Open dashboard" (→ /app). */
export default function LandingPage() {
  return (
    <div className="min-h-screen bg-white">
      <LandingHeader />
      <main>
        <HeroSection />
        <AudienceSection />
        <HowItWorksSection />
        <HighlightsSection />
        <FeaturesSection />
        <PricingSection />
        <FinalCallToAction />
      </main>
      <LandingFooter />
    </div>
  );
}
