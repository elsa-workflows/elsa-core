/**
 * The remove button follows the pointer.
 *
 * The cases replay event sequences X6 raises when the pointer moves quickly: an edge is entered again
 * before it was left, or the pointer reaches another edge, a node, the blank canvas or the outside of
 * the graph without an `edge:mouseleave`. Each one used to leave a remove button behind.
 */
import {Edge} from '@antv/x6';
import {beforeEach, describe, expect, it} from 'vitest';
import {EdgeHoverEvents, registerEdgeHoverTools} from '../edge-hover-tools';

type Handler = (args: any) => void;

class FakeGraph {
    private readonly handlers = new Map<string, Handler[]>();

    readonly on = ((name: string, handler: Handler) => {
        this.handlers.set(name, [...(this.handlers.get(name) ?? []), handler]);
        return this;
    }) as unknown as EdgeHoverEvents['on'];

    emit(name: string, args: object = {}) {
        for (const handler of this.handlers.get(name) ?? [])
            handler(args);
    }

    listens(name: string) {
        return this.handlers.has(name);
    }
}

const newEdge = () => new Edge({source: {x: 0, y: 0}, target: {x: 100, y: 0}});
const toolNames = (edge: Edge) => (edge.getTools()?.items ?? []).map(item => typeof item === 'string' ? item : item.name);
const removeButtons = (edge: Edge) => toolNames(edge).filter(name => name === 'button-remove').length;

describe('registerEdgeHoverTools', () => {
    let graph: FakeGraph;
    let first: Edge;
    let second: Edge;

    beforeEach(() => {
        graph = new FakeGraph();
        first = newEdge();
        second = newEdge();
        registerEdgeHoverTools(graph, true);
    });

    it('shows the vertex handles and one remove button on the hovered edge', () => {
        graph.emit('edge:mouseenter', {edge: first});

        expect(toolNames(first)).toEqual(['vertices', 'button-remove']);
    });

    it('adds each tool once when an edge is entered again before it was left', () => {
        graph.emit('edge:mouseenter', {edge: first});
        graph.emit('edge:mouseenter', {edge: first});
        graph.emit('edge:mouseenter', {edge: first});

        expect(toolNames(first)).toEqual(['vertices', 'button-remove']);
    });

    it('removes the button when the pointer leaves the edge', () => {
        graph.emit('edge:mouseenter', {edge: first});
        graph.emit('edge:mouseleave', {edge: first});

        expect(removeButtons(first)).toBe(0);
    });

    it('moves the button when the pointer reaches another edge without leaving the first', () => {
        graph.emit('edge:mouseenter', {edge: first});
        graph.emit('edge:mouseenter', {edge: second});

        expect(removeButtons(first)).toBe(0);
        expect(removeButtons(second)).toBe(1);
    });

    it.each(['node:mouseenter', 'blank:mouseover', 'graph:mouseleave'])(
        'removes the button on %s when edge:mouseleave never fired',
        event => {
            graph.emit('edge:mouseenter', {edge: first});
            graph.emit(event);

            expect(removeButtons(first)).toBe(0);
        });

    it('forgets an edge that was removed while hovered', () => {
        graph.emit('edge:mouseenter', {edge: first});
        graph.emit('edge:removed', {edge: first});
        graph.emit('edge:mouseenter', {edge: second});

        expect(removeButtons(second)).toBe(1);
    });

    it('registers nothing when edges are not interactive', () => {
        const readOnly = new FakeGraph();
        registerEdgeHoverTools(readOnly, false);

        expect(readOnly.listens('edge:mouseenter')).toBe(false);
    });
});
