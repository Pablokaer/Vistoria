"use client";

import { useEffect, useRef, type ReactNode } from "react";

/** True when the element starts below the visible area, i.e. the visitor has not seen it yet. */
function startsBelowFold(element: HTMLElement): boolean {
  return element.getBoundingClientRect().top > window.innerHeight;
}

/**
 * Lets a block rise in once when it scrolls into view. Server-rendered visible; script only hides blocks that are
 * still below the fold, and never with prefers-reduced-motion (the CSS lives in globals.css, `[data-reveal]`).
 * Example: `<Reveal><FeatureCards /></Reveal>`
 */
export function Reveal({ children, className }: { children: ReactNode; className?: string }) {
  const ref = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const element = ref.current;
    if (!element || window.matchMedia("(prefers-reduced-motion: reduce)").matches || !startsBelowFold(element)) return;
    element.dataset.reveal = "hidden";
    const observer = new IntersectionObserver(([entry]) => {
      if (!entry.isIntersecting) return;
      element.dataset.reveal = "shown";
      observer.disconnect();
    }, { rootMargin: "0px 0px -8% 0px" });
    observer.observe(element);
    return () => observer.disconnect();
  }, []);
  return <div ref={ref} className={className}>{children}</div>;
}
