import { Newsreader } from "next/font/google";

// Editorial serif for public-page headlines only (the app itself stays on Inter). Self-hosted at build time by
// next/font, so no request goes to Google from the browser; only pages that render landing headlines load it.
// "latin" already covers Portuguese (á, ã, ç, õ…); latin-ext would add two more preloaded files for nothing.
export const displayFont = Newsreader({
  subsets: ["latin"],
  style: ["normal", "italic"],
  display: "swap",
  variable: "--font-newsreader",
});
