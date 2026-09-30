import Link from "next/link";
import { cx } from "@/lib/cx";

/** The InspectFlow mark: a roofline over a check — a property that has been inspected. */
export function BrandMark({ className }: { className?: string }) {
  return (
    <span aria-hidden="true" className={cx("grid h-8 w-8 shrink-0 place-items-center rounded-md bg-brand text-white shadow-card", className)}>
      <svg viewBox="0 0 24 24" className="h-5 w-5" fill="none" stroke="currentColor" strokeWidth={2} strokeLinecap="round" strokeLinejoin="round">
        <path d="M3.5 11 12 4.5l8.5 6.5" />
        <path d="M6 9.5V19.5h12V9.5" />
        <path d="m9 14.2 2.1 2.1L15.2 12" />
      </svg>
    </span>
  );
}

/**
 * Logo + name. Inside the app it links to the user's home (`/app`); on public pages to the landing page.
 * `tone="dark"` is for the navy sidebar.
 */
export function Brand({ href = "/", alwaysShowName, tone = "light" }: { href?: string; alwaysShowName?: boolean; tone?: "light" | "dark" }) {
  return (
    <Link href={href} className={cx("flex items-center gap-2.5 font-semibold tracking-tight", tone === "dark" ? "text-white" : "text-ink")}>
      <BrandMark />
      <span className={cx("text-[1.0625rem]", alwaysShowName ? "inline" : "hidden sm:inline")}>InspectFlow</span>
    </Link>
  );
}
