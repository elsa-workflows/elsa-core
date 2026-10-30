import {findGraph} from "./find-graph";

export function zoomToFit(graphId: string) {
    findGraph(graphId)?.zoomToFit({
        padding: 20,
        minScale: 0.5,
        maxScale: 3
    });
}
