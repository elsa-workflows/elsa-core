
import { useTranslation } from "@elsa-workflows/react-studio-sdk/localization";
import { ToneBadge } from "@elsa-workflows/react-studio-ui/common/status-badge";
import type { SecretStatus } from "../api";

const tones = {
  Active: "success",
  Retired: "neutral",
  Expired: "warning",
  Revoked: "danger",
  Deleted: "neutral",
} as const satisfies Record<SecretStatus, "neutral" | "info" | "success" | "warning" | "danger">;

export function SecretStatusBadge({ status }: { status: SecretStatus }) {
  useTranslation();
  return <ToneBadge tone={tones[status] ?? "neutral"}>{status}</ToneBadge>;
}
