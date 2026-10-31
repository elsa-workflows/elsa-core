import type { Node } from '@xyflow/react';

export type GuideAxis = 'v' | 'h'; // vertical or horizontal

export interface SnapGuide {
    axis: GuideAxis;
    // Flow-space coordinate the guide lives at (x for vertical, y for horizontal).
    value: number;
    // Flow-space span the guide should cover (min..max along the OTHER axis).
    from: number;
    to: number;
}

export interface SnapResult {
    position: { x: number; y: number };
    guides: SnapGuide[];
}

const SNAP_THRESHOLD = 6; // px in flow coordinates

interface NodeBox {
    id: string;
    left: number;
    right: number;
    centerX: number;
    top: number;
    bottom: number;
    centerY: number;
}

function box(node: Node): NodeBox | null {
    const w = (node.style as any)?.width ?? node.measured?.width;
    const h = (node.style as any)?.height ?? node.measured?.height;
    if (!w || !h) return null;
    const x = node.position.x;
    const y = node.position.y;
    return {
        id: node.id,
        left: x,
        right: x + w,
        centerX: x + w / 2,
        top: y,
        bottom: y + h,
        centerY: y + h / 2,
    };
}

/**
 * Computes alignment guides + a snapped position for the dragging node.
 * Compares left/right/centerX against every other node (vertical guides) and
 * top/bottom/centerY (horizontal guides). Picks the closest snap per axis.
 */
export function computeSnap(draggingNode: Node, allNodes: Node[]): SnapResult {
    const dragBox = box(draggingNode);
    if (!dragBox) return { position: draggingNode.position, guides: [] };

    const others = allNodes
        .map(n => (n.id === draggingNode.id ? null : box(n)))
        .filter((b): b is NodeBox => b !== null);

    const w = dragBox.right - dragBox.left;
    const h = dragBox.bottom - dragBox.top;

    // Find best vertical alignment (anchor on the dragging node).
    type Match = { delta: number; guide: SnapGuide; anchor: 'left' | 'right' | 'centerX' | 'top' | 'bottom' | 'centerY' };
    const vMatches: Match[] = [];
    const hMatches: Match[] = [];

    for (const other of others) {
        const otherTop = Math.min(dragBox.top, other.top);
        const otherBottom = Math.max(dragBox.bottom, other.bottom);
        const otherLeft = Math.min(dragBox.left, other.left);
        const otherRight = Math.max(dragBox.right, other.right);

        // Vertical alignments (same x).
        const vPairs: Array<{ a: number; b: number; anchor: Match['anchor'] }> = [
            { a: dragBox.left, b: other.left, anchor: 'left' },
            { a: dragBox.right, b: other.right, anchor: 'right' },
            { a: dragBox.centerX, b: other.centerX, anchor: 'centerX' },
            { a: dragBox.left, b: other.right, anchor: 'left' },
            { a: dragBox.right, b: other.left, anchor: 'right' },
        ];
        for (const p of vPairs) {
            const delta = p.b - p.a;
            if (Math.abs(delta) <= SNAP_THRESHOLD) {
                vMatches.push({
                    delta,
                    anchor: p.anchor,
                    guide: { axis: 'v', value: p.b, from: otherTop, to: otherBottom },
                });
            }
        }

        // Horizontal alignments (same y).
        const hPairs: Array<{ a: number; b: number; anchor: Match['anchor'] }> = [
            { a: dragBox.top, b: other.top, anchor: 'top' },
            { a: dragBox.bottom, b: other.bottom, anchor: 'bottom' },
            { a: dragBox.centerY, b: other.centerY, anchor: 'centerY' },
            { a: dragBox.top, b: other.bottom, anchor: 'top' },
            { a: dragBox.bottom, b: other.top, anchor: 'bottom' },
        ];
        for (const p of hPairs) {
            const delta = p.b - p.a;
            if (Math.abs(delta) <= SNAP_THRESHOLD) {
                hMatches.push({
                    delta,
                    anchor: p.anchor,
                    guide: { axis: 'h', value: p.b, from: otherLeft, to: otherRight },
                });
            }
        }
    }

    let dx = 0;
    let dy = 0;
    const guides: SnapGuide[] = [];

    if (vMatches.length > 0) {
        // Pick the smallest |delta|; collect all guides at that final value.
        vMatches.sort((a, b) => Math.abs(a.delta) - Math.abs(b.delta));
        dx = vMatches[0].delta;
        const snappedX = ({
            left: dragBox.left + dx,
            right: dragBox.right + dx,
            centerX: dragBox.centerX + dx,
        } as any)[vMatches[0].anchor];
        for (const m of vMatches) if (Math.abs(m.guide.value - snappedX) < 0.5) guides.push(m.guide);
    }
    if (hMatches.length > 0) {
        hMatches.sort((a, b) => Math.abs(a.delta) - Math.abs(b.delta));
        dy = hMatches[0].delta;
        const snappedY = ({
            top: dragBox.top + dy,
            bottom: dragBox.bottom + dy,
            centerY: dragBox.centerY + dy,
        } as any)[hMatches[0].anchor];
        for (const m of hMatches) if (Math.abs(m.guide.value - snappedY) < 0.5) guides.push(m.guide);
    }

    // De-duplicate guides (same axis + value).
    const dedup = new Map<string, SnapGuide>();
    for (const g of guides) {
        const key = `${g.axis}:${g.value.toFixed(2)}`;
        const existing = dedup.get(key);
        if (!existing) {
            dedup.set(key, g);
        } else {
            existing.from = Math.min(existing.from, g.from);
            existing.to = Math.max(existing.to, g.to);
        }
    }

    // Extend each guide to also include the dragging node's range so the line
    // visibly connects the two aligned nodes.
    const finalGuides = Array.from(dedup.values()).map(g => {
        if (g.axis === 'v') {
            return {
                ...g,
                from: Math.min(g.from, dragBox.top + dy),
                to: Math.max(g.to, dragBox.bottom + dy),
            };
        }
        return {
            ...g,
            from: Math.min(g.from, dragBox.left + dx),
            to: Math.max(g.to, dragBox.right + dx),
        };
    });

    void w; void h; // unused but kept to make intent explicit
    return {
        position: { x: dragBox.left + dx, y: dragBox.top + dy },
        guides: finalGuides,
    };
}
