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
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${apiUrl}/api/:path*` }];
  },
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default nextConfig;
