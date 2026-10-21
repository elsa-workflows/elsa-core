import { ToneBadge } from "@/components/common/status-badge";
import type { SecretStatus } from "../api";

const tones = {
  Active: "success",
  Retired: "neutral",
  Expired: "warning",
  Revoked: "danger",
  Deleted: "neutral",
} as const satisfies Record<SecretStatus, "neutral" | "info" | "success" | "warning" | "danger">;

export function SecretStatusBadge({ status }: { status: SecretStatus }) {
  return <ToneBadge tone={tones[status] ?? "neutral"}>{status}</ToneBadge>;
}
