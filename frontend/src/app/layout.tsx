import type { Metadata, Viewport } from "next";
import { AuthProvider } from "@/lib/auth";
import "./globals.css";

export const metadata: Metadata = {
  title: { default: "InspectFlow", template: "%s · InspectFlow" },
  description: "Property inspections for companies, inspectors and tenants.",
  referrer: "no-referrer",
};

export const viewport: Viewport = { width: "device-width", initialScale: 1, themeColor: "#0f4c5c" };

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className="h-full antialiased">
      <body className="min-h-full">
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
