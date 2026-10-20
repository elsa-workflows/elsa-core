import { defineModule } from "@elsa-workflows/react-studio-sdk";
import { ActivityPropertiesPanel } from "./activity-properties-panel";
import { ExpressionEditor } from "./expression-editor";
import { builtInEditors } from "./registry";
export const activityEditorsModule = defineModule({ id: "activity-editors", name: "activity-editors", core: true, extensions: { uiHints: builtInEditors, components: [
  { id: "ActivityPropertiesPanel", name: "ActivityPropertiesPanel", component: ActivityPropertiesPanel },
  { id: "ExpressionEditor", name: "ExpressionEditor", component: ExpressionEditor },
] } });
