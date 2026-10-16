import type { Metadata } from "next";
import { DispatchRoutes } from "@/components/landing/DispatchRoutes";
import { displayFont } from "@/components/landing/fonts";
import { LandingHero } from "@/components/landing/LandingHero";
import { FinalCallToAction, LandingFooter } from "@/components/landing/LandingFooter";
import { LandingHeader } from "@/components/landing/LandingHeader";
import { PricingSection } from "@/components/landing/PricingSection";
import { ReportGuarantees } from "@/components/landing/ReportGuarantees";
import { SignedInRedirect } from "@/components/landing/SignedInRedirect";
import { WorkflowLedger } from "@/components/landing/WorkflowLedger";
import { getServerTranslator } from "@/i18n/server";
import { landingMessages } from "@/i18n/messages/landing";
import { hasSessionHint } from "@/lib/session-hint";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getServerTranslator(landingMessages);
  const title = t("metaTitle");
  const description = t("metaDescription");
  return { title: { absolute: title }, description, openGraph: { title, description, type: "website", siteName: "InspectFlow" } };
}

/**
 * "/": the public landing page for visitors; signed-in users are sent to their home instead (see SignedInRedirect).
 * Only public content is rendered here, so nothing protected can leak through this route.
 */
export default async function LandingPage() {
  const sessionHint = await hasSessionHint();
  return (
    <SignedInRedirect sessionHint={sessionHint}>
      <div className={`${displayFont.variable} min-h-screen bg-paper text-ink-2`}>
        <LandingHeader />
        <main id="main">
          <LandingHero />
          <WorkflowLedger />
          <ReportGuarantees />
          <DispatchRoutes />
          <PricingSection />
          <FinalCallToAction />
        </main>
        <LandingFooter />
      </div>
    </SignedInRedirect>
  );
}
