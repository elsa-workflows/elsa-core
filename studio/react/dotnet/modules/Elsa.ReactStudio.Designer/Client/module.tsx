import { defineModule } from "@elsa-workflows/react-studio-sdk";
import { DesignerSurface } from "./components/designer-surface";
import { ActivityToolbox } from "./components/activity-toolbox";
import { StatePanel } from "./components/state-panel";
import { TransitionPanel } from "./components/transition-panel";
export const designerModule = defineModule({ id: "designer", name: "designer", core: true, extensions: { components: [
  { id: "DesignerSurface", name: "DesignerSurface", component: DesignerSurface },
  { id: "ActivityToolbox", name: "ActivityToolbox", component: ActivityToolbox },
  { id: "StatePanel", name: "StatePanel", component: StatePanel },
  { id: "TransitionPanel", name: "TransitionPanel", component: TransitionPanel },
] } });
