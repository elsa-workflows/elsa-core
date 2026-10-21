/** Small formatting helpers shared by the dashboard widgets. */

/** Parses a .NET `TimeSpan` string (`[-][d.]hh:mm:ss[.fffffff]`) into milliseconds. */
export function parseTimeSpan(value: string | null | undefined): number | undefined {
  if (!value) return undefined;
  const m = /^(-)?(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$/.exec(value.trim());
  if (!m) return undefined;
  const [, sign, days, hours, minutes, seconds, fraction] = m;
  const ms =
    (Number(days ?? 0) * 86_400 + Number(hours) * 3_600 + Number(minutes) * 60 + Number(seconds)) * 1_000 +
    (fraction ? Math.round(Number(`0.${fraction}`) * 1_000) : 0);
  return sign ? -ms : ms;
}

/** Human duration: `820 ms`, `4.2 s`, `3m 05s`, `2h 14m`, `3d 6h`. */
export function formatDuration(ms: number | undefined): string {
  if (ms === undefined || !Number.isFinite(ms)) return "–";
  if (ms < 1_000) return `${Math.round(ms)} ms`;
  const s = ms / 1_000;
  if (s < 60) return `${s.toFixed(s < 10 ? 1 : 0)} s`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m ${String(Math.round(s % 60)).padStart(2, "0")}s`;
  const h = Math.floor(m / 60);
  if (h < 48) return `${h}h ${String(m % 60).padStart(2, "0")}m`;
  const d = Math.floor(h / 24);
  return `${d}d ${h % 24}h`;
}

/** Compact integers for tiles: 1,284 / 12.9K / 4.2M. */
export function formatCount(value: number | undefined): string {
  if (value === undefined || !Number.isFinite(value)) return "–";
  if (Math.abs(value) < 10_000) return value.toLocaleString();
  return new Intl.NumberFormat(undefined, { notation: "compact", maximumFractionDigits: 1 }).format(value);
}

export const RANGE_LABELS = { "1h": "Last hour", "24h": "Last 24 hours", "7d": "Last 7 days" } as const;
