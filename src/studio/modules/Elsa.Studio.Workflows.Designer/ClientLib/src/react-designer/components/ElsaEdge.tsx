import { useState, useCallback, type MouseEvent as ReactMouseEvent } from 'react';
import {
    EdgeLabelRenderer,
    getSmoothStepPath,
    useReactFlow,
    Position,
    type EdgeProps,
} from '@xyflow/react';
import { useEdgeOps } from './EdgeOpsContext';

export interface ElsaEdgeData {
    vertices?: Array<{ x: number; y: number }>;
    structural?: boolean;
    [key: string]: unknown;
}

interface Point {
    x: number;
    y: number;
}

// Build an SVG path from a sequence of points with small rounded corners at
// each interior vertex. r=8 is subtle enough to not visually shift the
// segments while still smoothing the elbows.
function polylinePath(points: Point[], radius = 8): string {
    if (points.length === 0) return '';
    if (points.length === 1) return `M ${points[0].x} ${points[0].y}`;
    if (points.length === 2) return `M ${points[0].x} ${points[0].y} L ${points[1].x} ${points[1].y}`;

    let d = `M ${points[0].x} ${points[0].y}`;
    for (let i = 1; i < points.length - 1; i++) {
        const prev = points[i - 1];
        const cur = points[i];
        const next = points[i + 1];
        const v1 = { x: cur.x - prev.x, y: cur.y - prev.y };
        const v2 = { x: next.x - cur.x, y: next.y - cur.y };
        const len1 = Math.hypot(v1.x, v1.y) || 1;
        const len2 = Math.hypot(v2.x, v2.y) || 1;
        const r = Math.min(radius, len1 / 2, len2 / 2);
        const inX = cur.x - (v1.x / len1) * r;
        const inY = cur.y - (v1.y / len1) * r;
        const outX = cur.x + (v2.x / len2) * r;
        const outY = cur.y + (v2.y / len2) * r;
        d += ` L ${inX} ${inY} Q ${cur.x} ${cur.y} ${outX} ${outY}`;
    }
    const last = points[points.length - 1];
    d += ` L ${last.x} ${last.y}`;
    return d;
}

// Pick the segment of the polyline closest to (px, py) and return its index
// (1 = "between source and first vertex", etc). Used to insert a new vertex
// at the right slot when the user double-clicks on the edge body.
function closestSegmentIndex(points: Point[], px: number, py: number): number {
    let best = 0;
    let bestDist = Infinity;
    for (let i = 0; i < points.length - 1; i++) {
        const a = points[i];
        const b = points[i + 1];
        const dx = b.x - a.x;
        const dy = b.y - a.y;
        const len2 = dx * dx + dy * dy || 1;
        const t = Math.max(0, Math.min(1, ((px - a.x) * dx + (py - a.y) * dy) / len2));
        const cx = a.x + t * dx;
        const cy = a.y + t * dy;
        const d = Math.hypot(px - cx, py - cy);
        if (d < bestDist) {
            bestDist = d;
            best = i + 1; // insert between i and i+1 → vertices[best - 1]
        }
    }
    return best;
}

export function ElsaEdge(props: EdgeProps) {
    const {
        id,
        sourceX,
        sourceY,
        targetX,
        targetY,
        sourcePosition = Position.Right,
        targetPosition = Position.Left,
        style,
        markerEnd,
        animated,
        selected,
        data,
    } = props;

    const ops = useEdgeOps();
    const flow = useReactFlow();
    const vertices = (data as ElsaEdgeData | undefined)?.vertices ?? [];
    const structural = (data as ElsaEdgeData | undefined)?.structural === true;
    const [hovered, setHovered] = useState(false);

    // Path + label position. With no vertices, fall back to the smoothstep
    // routing so visual parity with the no-bend default is preserved.
    let pathD: string;
    let labelX: number;
    let labelY: number;
    if (vertices.length === 0) {
        const [smooth, lx, ly] = getSmoothStepPath({
            sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition,
        });
        pathD = smooth;
        labelX = lx;
        labelY = ly;
    } else {
        const points: Point[] = [{ x: sourceX, y: sourceY }, ...vertices, { x: targetX, y: targetY }];
        pathD = polylinePath(points);
        // Label at the centre of the middle segment.
        const mid = Math.floor(points.length / 2);
        labelX = (points[mid - 1].x + points[mid].x) / 2;
        labelY = (points[mid - 1].y + points[mid].y) / 2;
    }

    const onPathDoubleClick = useCallback((e: ReactMouseEvent<SVGPathElement>) => {
        e.stopPropagation();
        const flowPos = flow.screenToFlowPosition({ x: e.clientX, y: e.clientY });
        const points: Point[] = [{ x: sourceX, y: sourceY }, ...vertices, { x: targetX, y: targetY }];
        const segIdx = closestSegmentIndex(points, flowPos.x, flowPos.y);
        ops.addVertex(id, flowPos, segIdx - 1); // insert at vertices[segIdx - 1]
    }, [flow, id, ops, sourceX, sourceY, targetX, targetY, vertices]);

    const startVertexDrag = useCallback((vertexIndex: number) => (e: ReactMouseEvent<SVGCircleElement>) => {
        e.stopPropagation();
        e.preventDefault();
        const onMove = (moveE: MouseEvent) => {
            const flowPos = flow.screenToFlowPosition({ x: moveE.clientX, y: moveE.clientY });
            ops.updateVertex(id, vertexIndex, flowPos);
        };
        const onUp = () => {
            window.removeEventListener('mousemove', onMove);
            window.removeEventListener('mouseup', onUp);
            ops.snapshotEdges(flow.getEdges());
        };
        window.addEventListener('mousemove', onMove);
        window.addEventListener('mouseup', onUp);
    }, [flow, id, ops]);

    const onVertexContextMenu = useCallback((vertexIndex: number) => (e: ReactMouseEvent<SVGCircleElement>) => {
        e.stopPropagation();
        e.preventDefault();
        ops.removeVertex(id, vertexIndex);
    }, [id, ops]);

    const onDeleteClick = useCallback((e: ReactMouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        ops.deleteEdge(id);
    }, [id, ops]);

    const onInsertClick = useCallback((e: ReactMouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        ops.requestInsertActivity(id, e.clientX, e.clientY);
    }, [id, ops]);

    return (
        <g
            className={`react-flow__edge-elsa${selected ? ' selected' : ''}${animated ? ' animated' : ''}`}
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
        >
            {/* Render the breathing pulse FIRST so the path + arrowhead end up
                on top of it — the dot becomes a soft halo around the arrow tip. */}
            {animated && (
                <circle
                    className="elsa-react-flow-edge-pulse"
                    cx={targetX}
                    cy={targetY}
                />
            )}
            <path
                d={pathD}
                className="react-flow__edge-path"
                style={style}
                markerEnd={markerEnd}
                fill="none"
            />
            {/* Wider invisible hit area for hover detection + double-click to add vertex. */}
            <path
                d={pathD}
                fill="none"
                stroke="transparent"
                strokeWidth={20}
                className="elsa-react-flow-edge-hit"
                onDoubleClick={onPathDoubleClick}
            />

            {!structural && vertices.map((v, i) => (
                <circle
                    key={i}
                    cx={v.x}
                    cy={v.y}
                    r={5}
                    className="elsa-react-flow-edge-vertex"
                    onMouseDown={startVertexDrag(i)}
                    onContextMenu={onVertexContextMenu(i)}
                />
            ))}

            {hovered && (
                <EdgeLabelRenderer>
                    {/* Insert-activity (+) button — opens the activity picker
                        and splices the picked activity into this edge. Positioned
                        slightly to the LEFT of the midpoint so the delete (×)
                        button can sit at the centre without overlap. */}
                    {!ops.readOnly && !structural && (
                        <button
                            type="button"
                            className="elsa-react-flow-edge-insert nodrag nopan"
                            style={{
                                position: 'absolute',
                                transform: `translate(-50%, -50%) translate(${labelX - 16}px, ${labelY}px)`,
                            }}
                            onClick={onInsertClick}
                            onMouseDown={(e) => e.stopPropagation()}
                            title="Insert activity"
                            aria-label="Insert activity"
                        >
                            +
                        </button>
                    )}
                    {!ops.readOnly && (
                        <button
                            type="button"
                            className="elsa-react-flow-edge-remove nodrag nopan"
                            style={{
                                position: 'absolute',
                                transform: `translate(-50%, -50%) translate(${labelX + 16}px, ${labelY}px)`,
                            }}
                            onClick={onDeleteClick}
                            onMouseDown={(e) => e.stopPropagation()}
                            title="Remove edge"
                        >
                            ×
                        </button>
                    )}
                </EdgeLabelRenderer>
            )}
        </g>
    );
}
