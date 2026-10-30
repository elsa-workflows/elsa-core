/**
 * The fallback layout's own edge cases, driven directly rather than through a `.bpmn` fixture.
 *
 * A loop, a scope nothing reaches, and a boundary event whose host is missing are all documents a
 * modeller can produce but which no captured fixture contains -- and each is a shape where a naive
 * layered layout either loops forever or quietly drops a node. ../__fixtures__ covers the layout's
 * behaviour on real documents; this covers the ways it could fail to terminate or to place.
 */
import { describe, expect, it } from 'vitest';
import { computeFallbackLayout, layoutKey, type LayoutScope } from '../fallback-layout';

function scope(id: string, elements: LayoutScope['elements'], flows: LayoutScope['flows'] = []): LayoutScope {
    return { id, elements, flows };
}

function node(id: string, elementType = 'serviceTask', extra: Partial<LayoutScope['elements'][number]> = {}) {
    return { id, elementType, attachedToElementId: null, childScopeId: null, ...extra };
}

describe('computeFallbackLayout', () => {
    it('places a chain left to right, one column per layer', () => {
        const placements = computeFallbackLayout([
            scope('p', [node('a', 'startEvent'), node('b'), node('c', 'endEvent')], [
                { sourceRef: 'a', targetRef: 'b' },
                { sourceRef: 'b', targetRef: 'c' },
            ]),
        ], 'p');

        const a = placements.get(layoutKey('p', 'a'))!;
        const b = placements.get(layoutKey('p', 'b'))!;
        const c = placements.get(layoutKey('p', 'c'))!;

        expect(a.x).toBeLessThan(b.x);
        expect(b.x).toBeLessThan(c.x);
        expect(a).toMatchObject({ width: 36, height: 36 });
        expect(b).toMatchObject({ width: 100, height: 80 });
    });

    it('stacks a split\'s branches in the same column', () => {
        const placements = computeFallbackLayout([
            scope('p', [node('gate', 'exclusiveGateway'), node('left'), node('right')], [
                { sourceRef: 'gate', targetRef: 'left' },
                { sourceRef: 'gate', targetRef: 'right' },
            ]),
        ], 'p');

        const left = placements.get(layoutKey('p', 'left'))!;
        const right = placements.get(layoutKey('p', 'right'))!;

        expect(left.x).toBe(right.x);
        expect(left.y).toBeLessThan(right.y);
    });

    // Kahn's algorithm leaves every node on a cycle unranked; without the second pass this would
    // either loop or drop the whole loop body off the canvas.
    it('terminates and places every element of a process that loops back on itself', () => {
        const placements = computeFallbackLayout([
            scope('p', [node('a', 'startEvent'), node('b'), node('c'), node('d', 'endEvent')], [
                { sourceRef: 'a', targetRef: 'b' },
                { sourceRef: 'b', targetRef: 'c' },
                { sourceRef: 'c', targetRef: 'b' },
                { sourceRef: 'c', targetRef: 'd' },
            ]),
        ], 'p');

        expect(['a', 'b', 'c', 'd'].every(id => placements.has(layoutKey('p', id)))).toBe(true);
    });

    it('places a process made entirely of a cycle', () => {
        const placements = computeFallbackLayout([
            scope('p', [node('a'), node('b')], [
                { sourceRef: 'a', targetRef: 'b' },
                { sourceRef: 'b', targetRef: 'a' },
            ]),
        ], 'p');

        expect(placements.size).toBe(2);
    });

    it('grows a host to fit the scope it contains, however deep the nesting goes', () => {
        const placements = computeFallbackLayout([
            scope('outer', [node('host', 'subProcess', { childScopeId: 'middle' })]),
            scope('middle', [node('inner', 'subProcess', { childScopeId: 'innermost' })]),
            scope('innermost', [node('x'), node('y'), node('z')], [
                { sourceRef: 'x', targetRef: 'y' },
                { sourceRef: 'y', targetRef: 'z' },
            ]),
        ], 'outer');

        const host = placements.get(layoutKey('outer', 'host'))!;
        const inner = placements.get(layoutKey('middle', 'inner'))!;
        const z = placements.get(layoutKey('innermost', 'z'))!;

        expect(inner.x).toBeGreaterThan(host.x);
        expect(inner.x + inner.width).toBeLessThanOrEqual(host.x + host.width);
        expect(z.x + z.width).toBeLessThanOrEqual(inner.x + inner.width);
        expect(z.y + z.height).toBeLessThanOrEqual(inner.y + inner.height);
    });

    it('lays a boundary event out as an ordinary node when its host is not in the scope', () => {
        const placements = computeFallbackLayout([
            scope('p', [
                node('host'),
                node('orphan', 'boundaryEvent', { attachedToElementId: 'somewhere-else' }),
            ]),
        ], 'p');

        const host = placements.get(layoutKey('p', 'host'))!;
        const orphan = placements.get(layoutKey('p', 'orphan'))!;

        expect(orphan).toBeDefined();
        expect(orphan.y).toBeGreaterThan(host.y + host.height);
    });

    it('places nothing, rather than guessing a root, when the named root scope is not there', () => {
        expect(computeFallbackLayout([scope('p', [node('a')])], 'missing').size).toBe(0);
    });

    it('terminates on a scope tree that refers back to itself', () => {
        const placements = computeFallbackLayout([
            scope('p', [node('host', 'subProcess', { childScopeId: 'p' })]),
        ], 'p');

        expect(placements.has(layoutKey('p', 'host'))).toBe(true);
    });
});
