import type { Metadata } from "next";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { FinalCallToAction, LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { PricingSection } from "@/components/landing/PricingSection";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getServerTranslator(landingMessages);
  return { title: t("metaPricingTitle") };
}

export default function PricingPage() {
  return (
    <div className="min-h-screen bg-white">
      <LandingHeader />
      <main>
        <PricingSection />
        <FinalCallToAction />
      </main>
      <LandingFooter />
    </div>
  );
}
