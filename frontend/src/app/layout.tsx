import type { Metadata, Viewport } from "next";
import { I18nProvider } from "@/i18n/I18nProvider";
import { defineMessages } from "@/i18n/translate";
import { getServerLocale, getServerTranslator } from "@/i18n/server";
import { AuthProvider } from "@/lib/auth";
import "./globals.css";

const layoutMessages = defineMessages(
  { description: "Faster, organised and trustworthy property inspections: AI-assisted move-in and move-out reports for letting agencies, inspectors and tenants." },
  { description: "Vistorias de imóveis mais rápidas, organizadas e confiáveis: laudos de entrada e saída com apoio de IA para imobiliárias, vistoriadores e inquilinos." },
);

export async function generateMetadata(): Promise<Metadata> {
  const t = await getServerTranslator(layoutMessages);
  return {
    title: { default: "InspectFlow", template: "%s · InspectFlow" },
    description: t("description"),
    referrer: "no-referrer",
  };
}

export const viewport: Viewport = { width: "device-width", initialScale: 1, themeColor: "#1d4ed8" };

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const locale = await getServerLocale();
  return (
    <html lang={locale} className="h-full antialiased">
      <body className="min-h-full">
        <I18nProvider initialLocale={locale}>
          <AuthProvider>{children}</AuthProvider>
        </I18nProvider>
      </body>
    </html>
  );
}
