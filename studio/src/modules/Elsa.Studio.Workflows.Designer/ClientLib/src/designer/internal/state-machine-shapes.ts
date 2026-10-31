import {Graph} from '@antv/x6';
import {StateMachineStateShape, StateMachineTransitionShape} from '../api/designer-mode';
import {createDesignerPortGroups, designerPortMarkup} from './designer-ports';

export function registerStateMachineShapes() {
    Graph.registerNode(
        StateMachineStateShape,
        {
            inherit: 'rect',
            width: 220,
            height: 76,
            markup: [
                {tagName: 'rect', selector: 'body'},
                {tagName: 'rect', selector: 'status'},
                {tagName: 'text', selector: 'title'},
                {tagName: 'text', selector: 'meta'},
            ],
            attrs: {
                body: {
                    width: 220,
                    height: 76,
                    rx: 8,
                    ry: 8,
                    fill: 'var(--elsa-designer-node-surface)',
                    stroke: 'var(--elsa-designer-node-border)',
                    strokeWidth: 1,
                },
                status: {
                    x: 0,
                    y: 0,
                    width: 5,
                    height: 76,
                    rx: 3,
                    ry: 3,
                    fill: 'var(--elsa-designer-node-accent)',
                    stroke: 'none',
                },
                title: {
                    x: 18,
                    y: 31,
                    fill: 'var(--elsa-designer-node-text)',
                    fontSize: 14,
                    fontWeight: 600,
                    textAnchor: 'start',
                    textVerticalAnchor: 'middle',
                    textWrap: {
                        width: 182,
                        height: 20,
                        ellipsis: true,
                    },
                },
                meta: {
                    x: 18,
                    y: 52,
                    fill: 'var(--elsa-designer-node-muted)',
                    fontSize: 11,
                    fontWeight: 500,
                    textAnchor: 'start',
                    textVerticalAnchor: 'middle',
                    textWrap: {
                        width: 182,
                        height: 18,
                        ellipsis: true,
                    },
                },
            },
            portMarkup: designerPortMarkup,
            ports: {
                groups: createDesignerPortGroups(),
            },
        },
        true,
    );

    Graph.registerEdge(
        StateMachineTransitionShape,
        {
            inherit: 'edge',
            attrs: {
                line: {
                    stroke: 'var(--elsa-designer-edge)',
                    strokeWidth: 1.5,
                    targetMarker: {
                        name: 'classic',
                        size: 7,
                    },
                },
            },
            connector: {
                name: 'rounded',
                args: {
                    radius: 8,
                },
            },
            router: {
                // State transitions are already authored with explicit vertices and
                // may connect through the same node boundary. Manhattan routing
                // cannot resolve several of those paths and logs a fallback for
                // every edge, so use the deterministic orthogonal router directly.
                name: 'orth',
            },
        },
        true,
    );
}
