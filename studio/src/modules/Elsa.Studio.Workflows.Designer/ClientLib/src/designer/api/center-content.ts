import {findGraph} from "./find-graph";

export function centerContent(graphId: string) {
    findGraph(graphId)?.centerContent({
        padding: 20,
        useCellGeometry: true
    });
}
