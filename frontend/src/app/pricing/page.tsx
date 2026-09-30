import type { Metadata } from "next";
import { FinalCallToAction, LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { PricingSection } from "@/components/landing/PricingSection";

export const metadata: Metadata = { title: "Pricing" };

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
