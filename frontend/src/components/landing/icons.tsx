// Simple 24px stroke icons for the public pages (no icon dependency).
import type { ReactNode, SVGProps } from "react";

export type IconName =
  | "building" | "doorIn" | "doorOut" | "rooms" | "camera" | "sparkles" | "alert" | "compare" | "file"
  | "userCheck" | "history" | "check" | "arrowRight" | "menu" | "close" | "shield" | "clipboard" | "phone";

const PATHS: Record<IconName, ReactNode> = {
  building: <><path d="M4 21V5a1 1 0 0 1 1-1h9a1 1 0 0 1 1 1v16" /><path d="M15 9h4a1 1 0 0 1 1 1v11" /><path d="M8 8h3M8 12h3M8 16h3M3 21h18" /></>,
  doorIn: <><path d="M14 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4" /><path d="M3 12h11M10 8l4 4-4 4" /></>,
  doorOut: <><path d="M10 4H6a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h4" /><path d="M10 12h11M17 8l4 4-4 4" /></>,
  rooms: <><rect x="3" y="3" width="18" height="18" rx="1.5" /><path d="M3 12h9V3M12 12v9M12 16h9" /></>,
  camera: <><path d="M4 8a2 2 0 0 1 2-2h1.5l1.5-2h6l1.5 2H18a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2Z" /><circle cx="12" cy="12.5" r="3.5" /></>,
  sparkles: <><path d="M10 3l1.6 4.4L16 9l-4.4 1.6L10 15l-1.6-4.4L4 9l4.4-1.6Z" /><path d="M18 14l.8 2.2L21 17l-2.2.8L18 20l-.8-2.2L15 17l2.2-.8Z" /></>,
  alert: <><path d="M12 4 2.5 20h19Z" /><path d="M12 10v4M12 17h.01" /></>,
  compare: <><rect x="3" y="5" width="7" height="14" rx="1" /><rect x="14" y="5" width="7" height="14" rx="1" /><path d="M10 12h4" /></>,
  file: <><path d="M6 3h8l4 4v13a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" /><path d="M14 3v4h4M8 12h8M8 16h6" /></>,
  userCheck: <><circle cx="9" cy="8" r="4" /><path d="M2 21a7 7 0 0 1 14 0M16 11l2 2 4-4" /></>,
  history: <><path d="M3 12a9 9 0 1 0 3-6.7L3 8" /><path d="M3 3v5h5M12 7v5l3 2" /></>,
  check: <path d="m5 12.5 4.5 4.5L19 7.5" />,
  arrowRight: <path d="M5 12h14M13 6l6 6-6 6" />,
  menu: <path d="M4 7h16M4 12h16M4 17h16" />,
  close: <path d="M6 6l12 12M18 6 6 18" />,
  shield: <><path d="M12 3 5 6v5c0 4.5 3 8.5 7 10 4-1.5 7-5.5 7-10V6Z" /><path d="m9 12 2 2 4-4" /></>,
  clipboard: <><rect x="5" y="4" width="14" height="17" rx="1.5" /><path d="M9 4V3h6v1M9 11h6M9 15h4" /></>,
  phone: <><rect x="7" y="2.5" width="10" height="19" rx="2" /><path d="M11 18.5h2" /></>,
};

/** Example: `<Icon name="camera" className="h-5 w-5 text-brand" />` */
export function Icon({ name, ...props }: { name: IconName } & SVGProps<SVGSVGElement>) {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.75} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}>
      {PATHS[name]}
    </svg>
  );
}
