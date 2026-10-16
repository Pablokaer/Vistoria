import type { Metadata } from "next";
import { displayFont } from "@/components/landing/fonts";
import { FinalCallToAction, LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { PricingSection } from "@/components/landing/PricingSection";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getServerTranslator(landingMessages);
  return { title: t("metaPricingTitle") };
}

/** Plans on their own page; open to signed-in users too (the header then offers "Open dashboard"). */
export default function PricingPage() {
  return (
    <div className={`${displayFont.variable} min-h-screen bg-paper text-ink-2`}>
      <LandingHeader />
      <main id="main">
        <PricingSection />
        <FinalCallToAction />
      </main>
      <LandingFooter />
    </div>
  );
}
