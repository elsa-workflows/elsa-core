import { useViewport } from '@xyflow/react';
import type { SnapGuide } from '../internal/snap-lines';

interface SnapLinesProps {
    guides: SnapGuide[];
}

// Renders guide lines in flow space by applying the live viewport transform
// to each line's endpoints. Lives in screen space (pointer-events: none) so
// it never interferes with React Flow's own drag/click handling.
export function SnapLines({ guides }: SnapLinesProps) {
    const { x: vx, y: vy, zoom } = useViewport();
    if (guides.length === 0) return null;

    return (
        <svg
            className="elsa-react-flow-snap-lines"
            style={{
                position: 'absolute',
                inset: 0,
                width: '100%',
                height: '100%',
                pointerEvents: 'none',
                zIndex: 5,
            }}
        >
            {guides.map((g, i) => {
                if (g.axis === 'v') {
                    const screenX = g.value * zoom + vx;
                    const y1 = g.from * zoom + vy;
                    const y2 = g.to * zoom + vy;
                    return <line key={i} x1={screenX} x2={screenX} y1={y1} y2={y2} />;
                }
                const screenY = g.value * zoom + vy;
                const x1 = g.from * zoom + vx;
                const x2 = g.to * zoom + vx;
                return <line key={i} x1={x1} x2={x2} y1={screenY} y2={screenY} />;
            })}
        </svg>
    );
}
