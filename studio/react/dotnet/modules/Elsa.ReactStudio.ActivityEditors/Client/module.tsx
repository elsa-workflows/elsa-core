import { defineModule } from "@elsa-workflows/react-studio-sdk";
import { ActivityPropertiesPanel } from "./activity-properties-panel";
import { ExpressionEditor } from "./expression-editor";
import { builtInEditors } from "./registry";
export const activityEditorsModule = defineModule({ id: "activity-editors", name: "activity-editors", core: true, extensions: { uiHintContributions: Object.entries(builtInEditors).map(([hint, component]) => ({ id: hint, hint, component, order: 1000 })), components: [
  { id: "ActivityPropertiesPanel", name: "ActivityPropertiesPanel", component: ActivityPropertiesPanel },
  { id: "ExpressionEditor", name: "ExpressionEditor", component: ExpressionEditor },
] } });
