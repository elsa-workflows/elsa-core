"use client";

import { useMemo } from "react";
import { useDesignerState } from "../context";
import { containerKind } from "../model/containers";
import { resolveContainer } from "../model/flowchart-ops";
import { DesignerCanvas, type DesignerCanvasProps } from "./designer-canvas";
import { SequenceDesigner } from "./sequence-designer";
import { StateMachineCanvas } from "./state-machine-canvas";

/**
 * Picks the designer for the container currently opened (root or a nested one reached through the
 * breadcrumb): React Flow canvas for flowcharts and state machines, an ordered list for sequences.
 */
export function DesignerSurface(props: DesignerCanvasProps) {
  const root = useDesignerState((s) => s.root);
  const path = useDesignerState((s) => s.path);
  const kind = useMemo(() => containerKind(resolveContainer(root, path) ?? root), [root, path]);
  if (kind === "sequence") return <SequenceDesigner />;
  if (kind === "stateMachine") return <StateMachineCanvas minimap={props.minimap} onToggleMinimap={props.onToggleMinimap} onOpenToolbox={props.onOpenToolbox} />;
  return <DesignerCanvas {...props} />;
}
