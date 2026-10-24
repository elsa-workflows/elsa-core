/**
 * The X6 shapes the designer draws with. A shape declares its markup and the attributes every instance shares;
 * cells.ts sets what varies per element. Colours are CSS custom properties from x6-designer.css, and the part of a
 * shape that shows its outline carries the class `elsa-x6-outline`, so instance state and problem markers can recolour
 * it from the stylesheet.
 */
import { Graph } from '@antv/x6';

export const SHAPES = {
    event: 'elsa-x6-event',
    gateway: 'elsa-x6-gateway',
    activity: 'elsa-x6-activity',
    container: 'elsa-x6-container',
    data: 'elsa-x6-data',
    annotation: 'elsa-x6-annotation',
    group: 'elsa-x6-group',
    unknown: 'elsa-x6-unknown',
    label: 'elsa-x6-label',
    badge: 'elsa-x6-badge',
    problem: 'elsa-x6-problem',
    handle: 'elsa-x6-handle',
    replace: 'elsa-x6-replace',
    edge: 'elsa-x6-edge'
} as const;

export const STROKE = 'var(--elsa-x6-stroke)';
export const SURFACE = 'var(--elsa-x6-surface)';
export const TEXT = 'var(--elsa-x6-text)';
export const MUTED = 'var(--elsa-x6-muted)';
const CONTAINER_SURFACE = 'var(--elsa-x6-container-surface)';
const HEADER_SURFACE = 'var(--elsa-x6-header-surface)';

const OUTLINE = 'elsa-x6-outline';

const text = (extra: Record<string, unknown> = {}) => ({
    text: '',
    fill: TEXT,
    fontSize: 12,
    fontFamily: 'inherit',
    textAnchor: 'middle',
    textVerticalAnchor: 'middle',
    refX: '50%',
    refY: '50%',
    pointerEvents: 'none',
    ...extra
});

const glyph = {
    display: 'none',
    d: '',
    fill: 'none',
    stroke: STROKE,
    strokeWidth: 1.2,
    strokeLinejoin: 'round',
    strokeLinecap: 'round',
    pointerEvents: 'none'
};

let registered = false;

/** Registers the shapes once. X6's registry is global, so a second designer on the page reuses them. */
export function registerShapes(): void {
    if (registered) return;
    registered = true;

    Graph.registerNode(SHAPES.event, {
        markup: [
            { tagName: 'circle', selector: 'body', className: OUTLINE },
            { tagName: 'circle', selector: 'ring' },
            { tagName: 'path', selector: 'icon' }
        ],
        attrs: {
            body: { refCx: '50%', refCy: '50%', refR: '50%', fill: SURFACE, stroke: STROKE, strokeWidth: 1.5 },
            ring: { display: 'none', refCx: '50%', refCy: '50%', refR: '41%', fill: 'none', stroke: STROKE, strokeWidth: 1.5, pointerEvents: 'none' },
            icon: { ...glyph, refX: '50%', refY: '50%' }
        }
    }, true);

    Graph.registerNode(SHAPES.gateway, {
        markup: [
            { tagName: 'polygon', selector: 'body', className: OUTLINE },
            { tagName: 'path', selector: 'marker' }
        ],
        attrs: {
            body: { refPoints: '0,0.5 0.5,0 1,0.5 0.5,1', fill: SURFACE, stroke: STROKE, strokeWidth: 1.5 },
            marker: { ...glyph, refX: '50%', refY: '50%' }
        }
    }, true);

    // Tasks, subprocesses and call activities differ only in border, markers and where the name sits.
    Graph.registerNode(SHAPES.activity, {
        markup: [
            { tagName: 'rect', selector: 'body', className: OUTLINE },
            { tagName: 'rect', selector: 'innerBorder' },
            { tagName: 'path', selector: 'typeMarker' },
            { tagName: 'text', selector: 'label' },
            { tagName: 'path', selector: 'markerA' },
            { tagName: 'path', selector: 'markerB' },
            { tagName: 'path', selector: 'markerC' },
            { tagName: 'path', selector: 'markerD' },
            { tagName: 'g', selector: 'drill', children: [{ tagName: 'rect', selector: 'drillButton' }, { tagName: 'path', selector: 'drillIcon' }] }
        ],
        attrs: {
            body: { refWidth: '100%', refHeight: '100%', rx: 10, ry: 10, fill: SURFACE, stroke: STROKE, strokeWidth: 1.5 },
            innerBorder: { display: 'none', x: 3, y: 3, refWidth: -6, refHeight: -6, rx: 8, ry: 8, fill: 'none', stroke: STROKE, strokeWidth: 1.5, pointerEvents: 'none' },
            typeMarker: { ...glyph, refX: 14, refY: 14 },
            label: text({ textWrap: { width: -16, height: -12, ellipsis: true } }),
            markerA: { ...glyph, refX: '50%', refY: '100%', refY2: -10 },
            markerB: { ...glyph, refX: '50%', refY: '100%', refY2: -10 },
            markerC: { ...glyph, refX: '50%', refY: '100%', refY2: -10 },
            markerD: { ...glyph, refX: '50%', refY: '100%', refY2: -10 },
            // Opens the plane a collapsed subprocess draws its content on. The designer listens for `node:drill`.
            drill: { display: 'none', event: 'node:drill', title: 'Open subprocess', cursor: 'pointer', refX: '100%', refX2: -20, refY: 4 },
            drillButton: { width: 16, height: 16, rx: 3, ry: 3, fill: SURFACE, stroke: MUTED, strokeWidth: 1 },
            drillIcon: { d: 'M 4 8 L 12 8 M 8 4 L 12 8 L 8 12', fill: 'none', stroke: MUTED, strokeWidth: 1.5, strokeLinecap: 'round', strokeLinejoin: 'round' }
        }
    }, true);

    // Pools and lanes: a body with a header band that carries the name, read bottom to top.
    Graph.registerNode(SHAPES.container, {
        markup: [
            { tagName: 'rect', selector: 'body', className: OUTLINE },
            { tagName: 'rect', selector: 'header' },
            { tagName: 'text', selector: 'label' }
        ],
        attrs: {
            body: { refWidth: '100%', refHeight: '100%', fill: CONTAINER_SURFACE, stroke: STROKE, strokeWidth: 1.5 },
            header: { width: 30, refHeight: '100%', fill: HEADER_SURFACE, stroke: STROKE, strokeWidth: 1.5 },
            label: text({ refX: 15, fontWeight: 600 })
        }
    }, true);

    // Data objects and data stores: a path scaled to the shape's bounds.
    Graph.registerNode(SHAPES.data, {
        markup: [{ tagName: 'path', selector: 'body', className: OUTLINE }],
        attrs: { body: { refDResetOffset: '', fill: SURFACE, stroke: STROKE, strokeWidth: 1.5 } }
    }, true);

    Graph.registerNode(SHAPES.annotation, {
        markup: [
            { tagName: 'rect', selector: 'hit' },
            { tagName: 'path', selector: 'body', className: OUTLINE },
            { tagName: 'text', selector: 'label' }
        ],
        attrs: {
            hit: { refWidth: '100%', refHeight: '100%', fill: 'transparent', stroke: 'none' },
            body: { d: '', fill: 'none', stroke: STROKE, strokeWidth: 1.5 },
            label: text({ refX: 6, textAnchor: 'start', textWrap: { width: -10, height: -6, ellipsis: true } })
        }
    }, true);

    Graph.registerNode(SHAPES.group, {
        markup: [
            { tagName: 'rect', selector: 'body', className: OUTLINE },
            { tagName: 'text', selector: 'label' }
        ],
        attrs: {
            body: { refWidth: '100%', refHeight: '100%', rx: 10, ry: 10, fill: 'none', stroke: STROKE, strokeWidth: 1.5, strokeDasharray: '8 4 1 4' },
            label: text({ refY: 4, textVerticalAnchor: 'top' })
        }
    }, true);

    // An element type the designer does not draw yet: a dashed box that names the type, so nothing disappears.
    Graph.registerNode(SHAPES.unknown, {
        markup: [
            { tagName: 'rect', selector: 'body', className: OUTLINE },
            { tagName: 'text', selector: 'label' }
        ],
        attrs: {
            body: { refWidth: '100%', refHeight: '100%', fill: SURFACE, stroke: MUTED, strokeWidth: 1, strokeDasharray: '4 3' },
            label: text({ fill: MUTED, fontSize: 11, textWrap: { width: -8, height: -8, ellipsis: true } })
        }
    }, true);

    // An element's name drawn where its DI places its label: events, gateways, data and flows. A click anywhere in its
    // bounds selects the element, through the transparent hit area: the text alone would leave gaps between letters.
    Graph.registerNode(SHAPES.label, {
        markup: [{ tagName: 'rect', selector: 'hit' }, { tagName: 'text', selector: 'label' }],
        attrs: {
            hit: { refWidth: '100%', refHeight: '100%', fill: 'transparent', stroke: 'none' },
            label: text({ fontSize: 11, textWrap: { width: '100%', height: '100%' } })
        }
    }, true);

    Graph.registerNode(SHAPES.badge, {
        markup: [
            { tagName: 'g', selector: 'group', children: [{ tagName: 'rect', selector: 'body' }, { tagName: 'text', selector: 'label' }] }
        ],
        attrs: {
            body: { refWidth: '100%', refHeight: '100%', rx: 9, ry: 9, stroke: 'none' },
            label: text({ fill: '#ffffff', fontSize: 11, fontWeight: 600, pointerEvents: 'none' })
        }
    }, true);

    // A corner of the selected shape, dragged to resize it.
    Graph.registerNode(SHAPES.handle, {
        markup: [{ tagName: 'rect', selector: 'body' }],
        attrs: { body: { refWidth: '100%', refHeight: '100%', fill: SURFACE, stroke: 'var(--elsa-x6-selection)', strokeWidth: 1.5 } }
    }, true);

    // Beside the selected element, opens the menu that replaces it with another type: a wrench, as in the bpmn-js
    // designer's context pad.
    Graph.registerNode(SHAPES.replace, {
        markup: [{ tagName: 'rect', selector: 'body' }, { tagName: 'path', selector: 'icon' }],
        attrs: {
            body: {
                refWidth: '100%',
                refHeight: '100%',
                rx: 3,
                ry: 3,
                fill: SURFACE,
                stroke: 'var(--elsa-x6-selection)',
                strokeWidth: 1,
                cursor: 'pointer',
                title: 'Change type (R)',
                role: 'button',
                'aria-label': 'Change type'
            },
            icon: {
                d: 'M 11.5 3 A 3.5 3.5 0 0 0 8.2 7.6 L 3.4 12.4 L 5.6 14.6 L 10.4 9.8 A 3.5 3.5 0 0 0 15 6.5 L 13 8.5 L 11 8.5 L 9.5 7 L 9.5 5 Z',
                fill: 'var(--elsa-x6-selection)',
                pointerEvents: 'none'
            }
        }
    }, true);

    Graph.registerNode(SHAPES.problem, {
        markup: [
            { tagName: 'g', selector: 'group', children: [{ tagName: 'circle', selector: 'body' }, { tagName: 'text', selector: 'label' }] }
        ],
        attrs: {
            body: { refCx: '50%', refCy: '50%', refR: '50%', stroke: '#ffffff', strokeWidth: 1.5 },
            label: text({ text: '!', fill: '#ffffff', fontSize: 12, fontWeight: 700 })
        }
    }, true);

    // Edges follow the document's waypoints exactly: the ends are points, not ports, and nothing re-routes them.
    Graph.registerEdge(SHAPES.edge, {
        markup: [
            { tagName: 'path', selector: 'wrap', groupSelector: 'lines' },
            { tagName: 'path', selector: 'line', groupSelector: 'lines', className: OUTLINE }
        ],
        attrs: {
            lines: { connection: true, strokeLinejoin: 'round', fill: 'none' },
            wrap: { strokeWidth: 10, stroke: 'transparent', cursor: 'pointer' },
            line: { stroke: STROKE, strokeWidth: 1.5, targetMarker: null, sourceMarker: null }
        },
        router: { name: 'normal' },
        connector: { name: 'normal' }
    }, true);
}
