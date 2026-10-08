import type {Cell} from '@antv/x6';
import {graphBindings} from "./graph-bindings";
import {isPersistentDesignerCell} from './designer-mode';

export function readGraph(graphId: string): {
    cells: Cell.Properties[];
    layoutOrientation?: string;
} {
    const {graph, layoutOrientation, mode} = graphBindings[graphId];
    const model = graph.toJSON();

    model.cells = model.cells.filter((cell: Cell.Properties) => isPersistentDesignerCell(cell, mode));

    (model as any).layoutOrientation = layoutOrientation;
    return model;
}
