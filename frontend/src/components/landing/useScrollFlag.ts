"use client";

import { useEffect, useRef, type RefObject } from "react";

/**
 * Marks the element with `data-scrolled="true"` once the page has scrolled past `threshold` pixels, so CSS can change
 * its look (Tailwind `data-[scrolled=true]:`) without re-rendering on every scroll event.
 * Example: `const headerRef = useScrollFlag<HTMLElement>(); <header ref={headerRef} …>`
 */
export function useScrollFlag<T extends HTMLElement>(threshold = 8): RefObject<T | null> {
  const ref = useRef<T>(null);
  useEffect(() => {
    // Scroll fires many times per frame; only touch the DOM when the flag actually flips.
    const update = () => {
      const scrolled = String(window.scrollY > threshold);
      if (ref.current && ref.current.dataset.scrolled !== scrolled) ref.current.dataset.scrolled = scrolled;
    };
    update();
    window.addEventListener("scroll", update, { passive: true });
    return () => window.removeEventListener("scroll", update);
  }, [threshold]);
  return ref;
}
