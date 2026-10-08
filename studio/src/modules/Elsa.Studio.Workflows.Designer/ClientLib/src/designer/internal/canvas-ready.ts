import {Graph} from '@antv/x6';

/**
 * Resolves once the graph's container has a non-zero height, or false if the graph went away first.
 *
 * Blazor renders the designer's container before the layout that gives it a height, and a
 * `centerContent` or `zoomToFit` against a zero-height container silently centres on nothing. The
 * `isCurrent` predicate is what stops a stale load from stealing the viewport of the graph that
 * replaced it: the caller answers whether the binding it started with is still the live one.
 */
export function whenCanvasHasHeight(graph: Graph, isCurrent: () => boolean): Promise<boolean> {
    const container = graph.container;

    return new Promise(resolve => {
        const checkSize = () => {
            if (!container.isConnected || !isCurrent()) {
                resolve(false);
                return;
            }

            if (container.getBoundingClientRect().height == 0)
                window.requestAnimationFrame(checkSize);
            else
                resolve(true);
        };

        checkSize();
    });
}
