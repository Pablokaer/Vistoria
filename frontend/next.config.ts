import type { NextConfig } from "next";

// All API traffic goes through the same origin (/api/*) so the refresh-token cookie stays
// HttpOnly + SameSite=Strict and no CORS is needed. API_INTERNAL_URL is read at build time.
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  // Invitation and share links carry tokens in the path: never leak them via Referer.
  { key: "Referrer-Policy", value: "no-referrer" },
  { key: "Permissions-Policy", value: "camera=(self), geolocation=()" },
];

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  images: {
    // AVIF first (typically 20-30% smaller than WebP), WebP for browsers without it.
    formats: ["image/avif", "image/webp"],
    // Next 16 only serves listed qualities: 75 is the default; 50 is for photos under a heavy veil (FinalCallToAction).
    qualities: [50, 75],
  },
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${apiUrl}/api/:path*` }];
  },
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default nextConfig;
