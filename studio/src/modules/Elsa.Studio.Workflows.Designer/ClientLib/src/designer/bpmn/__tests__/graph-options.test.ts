/**
 * Read-only means read-only.
 *
 * W9c renders a BPMN diagram; W14 is what makes one editable. Every interaction X6 offers therefore
 * has to be refused, and the refusal has to be checkable -- a graph configured inside a
 * `new Graph(...)` call can only be reviewed by reading it, and a single flag flipped by an X6
 * upgrade or a copy-paste from the flowchart designer would make the canvas quietly editable.
 */
import { describe, expect, it } from 'vitest';
import { BPMN_READ_ONLY_INTERACTING, createBpmnGraphOptions } from '../graph-options';

describe('createBpmnGraphOptions', () => {
    const options = createBpmnGraphOptions();

    it('refuses every interaction X6 knows how to offer', () => {
        const interacting = options.interacting as Record<string, unknown>;

        expect(Object.keys(interacting).length).toBeGreaterThan(0);

        for (const [name, value] of Object.entries(interacting)) {
            // `false`, not a predicate that happens to return false today.
            expect(value, name).toBe(false);
        }
    });

    it('names every interaction X6 has, so a new one cannot slip in unrefused', () => {
        expect(Object.keys(BPMN_READ_ONLY_INTERACTING).sort()).toEqual([
            'arrowheadMovable',
            'edgeLabelMovable',
            'edgeMovable',
            'magnetConnectable',
            'nodeMovable',
            'stopDelegateOnDragging',
            'toolsAddable',
            'useEdgeTools',
            'vertexAddable',
            'vertexDeletable',
            'vertexMovable',
        ]);
    });

    it('refuses to connect anything, whatever else is enabled', () => {
        const connecting = options.connecting!;

        expect(connecting.allowBlank).toBe(false);
        expect(connecting.allowLoop).toBe(false);
        expect(connecting.allowNode).toBe(false);
        expect(connecting.allowEdge).toBe(false);
        expect(connecting.allowPort).toBe(false);
        expect((connecting.validateMagnet as () => boolean)()).toBe(false);
        expect((connecting.validateConnection as () => boolean)()).toBe(false);
    });

    it('leaves X6 embedding off, so nothing on the canvas can reparent an element', () => {
        expect(options.embedding).toEqual({ enabled: false });
    });

    it('still allows the two things a reader needs', () => {
        expect(options.panning).toEqual({ enabled: true });
        expect((options.mousewheel as { enabled: boolean }).enabled).toBe(true);
    });

    it('lets the host turn panning, zooming and the grid off', () => {
        const quiet = createBpmnGraphOptions({ panning: false, mousewheel: false, grid: false });

        expect(quiet.panning).toEqual({ enabled: false });
        expect((quiet.mousewheel as { enabled: boolean }).enabled).toBe(false);
        expect(quiet.grid).toBe(false);
    });
});
