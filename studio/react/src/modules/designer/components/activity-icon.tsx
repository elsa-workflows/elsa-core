import { createElement } from "react";
import type { ActivityDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import { activityIcon } from "@elsa-workflows/react-studio-sdk/designer/model/icons";

/** Renders the icon for an activity type; looked up at render time without creating component types. */
export function ActivityIcon({ typeName, descriptor, className }: { typeName: string; descriptor?: ActivityDescriptor; className?: string }) {
  return createElement(activityIcon(typeName, descriptor), { className });
}
