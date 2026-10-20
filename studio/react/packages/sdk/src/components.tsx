import { createContext, useContext, type ComponentType, type ReactNode } from "react";
import type { CanvasDecorations } from "./designer/decorations.js";
import type { ActivityInput, Variable } from "@elsa-workflows/react-studio-api-client/types/activities";
import type { InputDescriptor } from "@elsa-workflows/react-studio-api-client/types/descriptors";
import type { OutputDefinition } from "@elsa-workflows/react-studio-api-client/types/workflow-definitions";
import type { UiHintContext } from "./editor-types.js";

export interface DesignerCanvasProps {
  className?: string;
  /** Shows the mini-map (useful in small panes to hide it). */
  minimap?: boolean;
  onToggleMinimap?: () => void;
  /** When the toolbox is hidden, offers a button to reopen it. */
  onOpenToolbox?: () => void;
  /** Extra toolbar buttons rendered next to the built-in ones. */
  toolbar?: ReactNode;
  /** Called when the user double-clicks a node (instance viewer opens details). */
  onNodeDoubleClick?: (activityId: string) => void;
  /** Node ids whose outgoing edges should be highlighted as traversed (instance viewer). */
  traversedActivityIds?: Set<string>;
  /** View-only nodes and edges drawn next to the activities (call-context box and external workflows). */
  decorations?: CanvasDecorations;
}

export interface ActivityPropertiesPanelProps {
  workflowDefinitionId?: string;
  variables: Variable[];
  workflowOutputs: OutputDefinition[];
  workflowInputs?: { name: string }[];
}
export interface ExpressionControlProps {
  descriptor: InputDescriptor;
  value: ActivityInput | undefined;
  onChange: (value: ActivityInput | undefined) => void;
  syntaxes: string[];
  readOnly?: boolean;
  children: (literal: { value: unknown; onChange: (value: unknown) => void }) => ReactNode;
  literalSyntax?: "Literal" | "Object";
  context?: UiHintContext;
}

/** Implementations supplied by the enabled Designer and ActivityEditors modules. */
export interface StudioComponentProps {
  Application: Record<string, never>;
  DesignerSurface: DesignerCanvasProps;
  ActivityToolbox: { className?: string; onCollapse?: () => void };
  StatePanel: { stateName: string };
  TransitionPanel: { index: number };
  ActivityPropertiesPanel: ActivityPropertiesPanelProps;
  ExpressionEditor: ExpressionControlProps;
}
export type StudioComponentMap = { [K in keyof StudioComponentProps]?: ComponentType<StudioComponentProps[K]> };
const ComponentsContext = createContext<StudioComponentMap>({});
export function StudioComponentsProvider({ components, children }: { components: StudioComponentMap; children: ReactNode }) {
  return <ComponentsContext.Provider value={components}>{children}</ComponentsContext.Provider>;
}
export function useStudioComponent<K extends keyof StudioComponentProps>(name: K): ComponentType<StudioComponentProps[K]> {
  const component = useContext(ComponentsContext)[name];
  if (!component) throw new Error(`Studio component ${name} requires its server feature to be enabled`);
  return component;
}
export function DesignerSurface(props: DesignerCanvasProps) { const Component = useStudioComponent("DesignerSurface"); return <Component {...props} />; }
export function Application() { const Component = useStudioComponent("Application"); return <Component />; }
export function ActivityToolbox(props: StudioComponentProps["ActivityToolbox"]) { const Component = useStudioComponent("ActivityToolbox"); return <Component {...props} />; }
export function StatePanel(props: StudioComponentProps["StatePanel"]) { const Component = useStudioComponent("StatePanel"); return <Component {...props} />; }
export function TransitionPanel(props: StudioComponentProps["TransitionPanel"]) { const Component = useStudioComponent("TransitionPanel"); return <Component {...props} />; }
export function ActivityPropertiesPanel(props: ActivityPropertiesPanelProps) { const Component = useStudioComponent("ActivityPropertiesPanel"); return <Component {...props} />; }
export function ExpressionEditor(props: ExpressionControlProps) { const Component = useStudioComponent("ExpressionEditor"); return <Component {...props} />; }
