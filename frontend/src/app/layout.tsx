import type { Metadata, Viewport } from "next";
import { AuthProvider } from "@/lib/auth";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "InspectFlow", template: "%s · InspectFlow" },
  description: "Faster, organised and trustworthy property inspections: AI-assisted move-in and move-out reports for letting agencies, inspectors and tenants.",
  referrer: "no-referrer",
};

export const viewport: Viewport = { width: "device-width", initialScale: 1, themeColor: "#1d4ed8" };

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="min-h-full">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
