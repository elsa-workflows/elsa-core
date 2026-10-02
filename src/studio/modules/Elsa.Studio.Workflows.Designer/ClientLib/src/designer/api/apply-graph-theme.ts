import {graphBindings} from "./graph-bindings";
import {resolveDesignerGridOptions} from "./grid-options";

export interface X6DesignerTheme {
    grid: string;
    edge: string;
    portSurface: string;
    portStroke: string;
    portText: string;
    selection: string;
    connectionHighlight: string;
    embeddingHighlight: string;
}

export function applyGraphTheme(graphId: string, theme: X6DesignerTheme) {
    const {graph, gridOptions} = graphBindings[graphId];
    applyDesignerThemeVariables(graph.container, theme);
    graph.drawGrid(resolveDesignerGridOptions(gridOptions, theme.grid));
}

export function applyDesignerThemeVariables(element: HTMLElement, theme: X6DesignerTheme) {
    const variables: Record<string, string> = {
        "--elsa-designer-grid": theme.grid,
        "--elsa-designer-edge": theme.edge,
        "--elsa-designer-port-surface": theme.portSurface,
        "--elsa-designer-port-stroke": theme.portStroke,
        "--elsa-designer-port-text": theme.portText,
        "--elsa-designer-selection": theme.selection,
        "--elsa-designer-connection-highlight": theme.connectionHighlight,
        "--elsa-designer-embedding-highlight": theme.embeddingHighlight,
    };

    for (const [name, value] of Object.entries(variables))
        element.style.setProperty(name, value);
}
