import { cn } from "@/lib/utils";

/*
 * Decorative workflow diagram drawn with theme colours; purely presentational.
 *
 * Motion: a pulse travels along each connection. Every connection path is normalised with
 * `pathLength="100"`, so one pulse crosses any connection in the same time; stages are delayed so the
 * pulse leaving one node arrives at the next and continues from there, and each node flashes when a
 * pulse reaches it. Keyframes live in globals.css (`elsa-flow`, `elsa-node-pulse`) and are disabled
 * under `prefers-reduced-motion`.
 */
const CYCLE_S = 8;
/** Fraction of the cycle one stage takes: the pulse (14 long) crosses 100 units of a 400-unit period. */
const STAGE = 114 / 400;
const stageDelay = (stage: number) => `${stage * STAGE * CYCLE_S}s`;

interface Link {
  d: string;
  /** Which hand-over step this connection carries. */
  stage: number;
  dashed?: boolean;
}

const links: Link[] = [
  // HTTP Endpoint → Validate order
  { d: "M172 55 H 236", stage: 0 },
  // Timer → If (joins the flow at the same moment as the loop from Validate order)
  { d: "M172 150 H 236", stage: 1 },
  // Validate order → If (loops down on the right)
  { d: "M408 55 H 430 Q 440 55 440 65 V 92 Q 440 102 430 102 H 214 Q 204 102 204 112 V 140 Q 204 150 214 150 H 236", stage: 1, dashed: true },
  // If → Send email
  { d: "M408 150 H 430 Q 440 150 440 160 V 187 Q 440 197 430 197 H 214 Q 204 197 204 207 V 235 Q 204 245 214 245 H 236", stage: 2 },
  // Send email → Finish
  { d: "M408 245 H 440 Q 450 245 450 237 H 470", stage: 3 },
];

export function LoginIllustration({ className }: { className?: string }) {
  const node = (x: number, y: number, w: number, label: string, sub: string, pulseStage: number, accent?: string) => (
    <g key={label} transform={`translate(${x} ${y})`}>
      <rect width={w} height="54" rx="10" fill="var(--card)" stroke="var(--border)" />
      {/* Flash when a pulse arrives. */}
      <rect
        width={w}
        height="54"
        rx="10"
        fill="none"
        stroke={accent ?? "var(--primary)"}
        strokeWidth="2"
        className="elsa-node-pulse"
        style={{ animationDelay: stageDelay(pulseStage), animationDuration: `${CYCLE_S}s` }}
      />
      <rect x="0" y="12" width="3" height="30" rx="2" fill={accent ?? "var(--primary)"} />
      <rect x="14" y="14" width="26" height="26" rx="7" fill={`color-mix(in oklab, ${accent ?? "var(--primary)"} 14%, var(--card))`} />
      <circle cx="27" cy="27" r="4" fill="none" stroke={accent ?? "var(--primary)"} strokeWidth="2" />
      <text x="50" y="26" fontSize="12" fontWeight="600" fill="var(--foreground)">{label}</text>
      <text x="50" y="42" fontSize="10" fill="var(--muted-foreground)">{sub}</text>
      <circle cx={w} cy="27" r="4.5" fill="var(--primary)" stroke="var(--card)" strokeWidth="2" />
      {x > 0 && <circle cx="0" cy="27" r="4.5" fill="var(--muted-foreground)" stroke="var(--card)" strokeWidth="2" />}
    </g>
  );
  return (
    <svg viewBox="0 0 560 300" className={cn("drop-shadow-sm", className)} aria-hidden>
      <defs>
        <marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
          <path d="M0 0L10 5L0 10z" fill="var(--muted-foreground)" />
        </marker>
      </defs>
      {/* Connections: muted so the travelling pulse stands out. */}
      <g fill="none" stroke="var(--muted-foreground)" strokeWidth="2" markerEnd="url(#arrow)" opacity="0.5">
        {links.map((l) => (
          <path key={l.d} d={l.d} strokeDasharray={l.dashed ? "6 4" : undefined} />
        ))}
      </g>
      {/* Travelling pulses (soft halo + bright core), drawn above the lines and below the nodes. */}
      <g fill="none" stroke="var(--primary)" strokeLinecap="round">
        {links.map((l) => {
          const style = { animationDelay: stageDelay(l.stage), animationDuration: `${CYCLE_S}s` };
          return (
            <g key={l.d}>
              <path d={l.d} pathLength={100} strokeWidth="9" className="elsa-flow-pulse elsa-flow-pulse-halo" style={style} />
              <path d={l.d} pathLength={100} strokeWidth="3.5" className="elsa-flow-pulse" style={style} />
            </g>
          );
        })}
      </g>
      {node(0, 28, 172, "HTTP Endpoint", "Trigger · POST /orders", 0, "var(--status-warning)")}
      {node(236, 28, 172, "Validate order", "JavaScript", 1)}
      {node(236, 123, 172, "If", "Branching", 2, "var(--elsa-secondary)")}
      {node(0, 123, 172, "Timer", "Trigger · every 5m", 1, "var(--status-warning)")}
      {node(236, 218, 172, "Send email", "Email · customer", 3, "var(--status-success)")}
      <g transform="translate(470 210)">
        <rect width="86" height="54" rx="10" fill="var(--card)" stroke="var(--border)" />
        <rect width="86" height="54" rx="10" fill="none" stroke="var(--status-danger)" strokeWidth="2" className="elsa-node-pulse" style={{ animationDelay: stageDelay(4), animationDuration: `${CYCLE_S}s` }} />
        <rect x="0" y="12" width="3" height="30" rx="2" fill="var(--status-danger)" />
        <text x="16" y="33" fontSize="12" fontWeight="600" fill="var(--foreground)">Finish</text>
        <circle cx="0" cy="27" r="4.5" fill="var(--muted-foreground)" stroke="var(--card)" strokeWidth="2" />
      </g>
    </svg>
  );
}
