// Stylised room "photos" for the product mockups (vector, so they stay crisp and weigh nothing).

type RoomVariant = "living" | "kitchen" | "bedroom";

const PALETTE: Record<RoomVariant, { wall: string; side: string; floor: string; item: string; accent: string }> = {
  living: { wall: "#eef1f5", side: "#e2e7ee", floor: "#c9a77c", item: "#64748b", accent: "#93c5fd" },
  kitchen: { wall: "#f1f5f9", side: "#e5eaf0", floor: "#cbd5e1", item: "#94a3b8", accent: "#bfdbfe" },
  bedroom: { wall: "#f3f1ee", side: "#e8e4df", floor: "#b8a18a", item: "#a5b4fc", accent: "#dbeafe" },
};

/** Example: `<RoomSketch variant="kitchen" scuff className="h-24 w-full" />` — `scuff` draws a wall defect. */
export function RoomSketch({ variant, scuff, className }: { variant: RoomVariant; scuff?: boolean; className?: string }) {
  const c = PALETTE[variant];
  return (
    <svg viewBox="0 0 160 120" className={className} role="img" aria-label={`${variant} photo`} preserveAspectRatio="xMidYMid slice">
      <rect width="160" height="120" fill={c.wall} />
      <polygon points="0,0 28,0 28,84 0,120" fill={c.side} />
      <polygon points="160,0 132,0 132,84 160,120" fill={c.side} />
      <polygon points="0,120 28,84 132,84 160,120" fill={c.floor} />
      <rect x="62" y="20" width="36" height="30" fill="#fff" />
      <rect x="65" y="23" width="14" height="24" fill={c.accent} />
      <rect x="81" y="23" width="14" height="24" fill={c.accent} />
      <Furniture variant={variant} color={c.item} />
      {scuff && (
        <g stroke="#78716c" strokeWidth="1.2" strokeLinecap="round">
          <path d="M36 58l16-1M38 61l13 1M40 64l12-2" />
        </g>
      )}
    </svg>
  );
}

function Furniture({ variant, color }: { variant: RoomVariant; color: string }) {
  if (variant === "kitchen") {
    return (
      <g>
        <rect x="30" y="62" width="100" height="22" fill={color} />
        <rect x="30" y="58" width="100" height="4" fill="#e2e8f0" />
        <rect x="100" y="28" width="28" height="20" fill={color} opacity="0.7" />
      </g>
    );
  }
  if (variant === "bedroom") {
    return (
      <g>
        <rect x="50" y="64" width="60" height="22" rx="2" fill={color} />
        <rect x="50" y="58" width="60" height="8" rx="2" fill="#fff" />
      </g>
    );
  }
  return (
    <g>
      <rect x="36" y="64" width="40" height="20" rx="2" fill={color} />
      <rect x="96" y="70" width="30" height="14" rx="2" fill="#94a3b8" />
    </g>
  );
}
