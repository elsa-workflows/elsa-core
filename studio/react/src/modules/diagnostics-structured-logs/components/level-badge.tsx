import { ToneBadge } from "@/components/common/status-badge";
import { levelName, type StructuredLogEvent } from "../api";

type Tone = "neutral" | "info" | "success" | "warning" | "danger";

export function levelTone(level: StructuredLogEvent["level"]): Tone {
  switch (levelName(level)) {
    case "Critical":
    case "Error":
      return "danger";
    case "Warning":
      return "warning";
    case "Information":
      return "info";
    default:
      return "neutral";
  }
}

const SHORT: Record<string, string> = { Trace: "TRC", Debug: "DBG", Information: "INF", Warning: "WRN", Error: "ERR", Critical: "CRT", None: "-" };

export function LevelBadge({ level }: { level: StructuredLogEvent["level"] }) {
  const name = levelName(level);
  return (
    <ToneBadge tone={levelTone(level)} className="font-mono text-[10px] px-1.5 py-0">
      <span title={name}>{SHORT[name] ?? name}</span>
    </ToneBadge>
  );
}
