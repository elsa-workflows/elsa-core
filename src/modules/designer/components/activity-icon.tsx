import { createElement } from "react";
import type { ActivityDescriptor } from "@/lib/api/types/descriptors";
import { activityIcon } from "../model/icons";

/** Renders the icon for an activity type; looked up at render time without creating component types. */
export function ActivityIcon({ typeName, descriptor, className }: { typeName: string; descriptor?: ActivityDescriptor; className?: string }) {
  return createElement(activityIcon(typeName, descriptor), { className });
}
