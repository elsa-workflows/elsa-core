import type { Activity } from "@/lib/api/types/activities";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";

export function activityDisplayText(activity: Activity, descriptor?: ActivityDescriptor) {
  const custom = activity.metadata?.displayText;
  if (typeof custom === "string" && custom.trim()) return custom;
  if (activity.name) return activity.name;
  return descriptor?.displayName ?? activity.type;
}

export function activityDescription(activity: Activity, descriptor?: ActivityDescriptor) {
  const custom = activity.metadata?.description;
  if (typeof custom === "string" && custom.trim()) return custom;
  return descriptor?.description;
}

export const DEFAULT_NODE_WIDTH = 220;
export const DEFAULT_NODE_HEIGHT = 56;

export function activityPosition(activity: Activity) {
  return activity.metadata?.designer?.position ?? { x: 0, y: 0 };
}

export function activitySize(activity: Activity) {
  return activity.metadata?.designer?.size;
}
