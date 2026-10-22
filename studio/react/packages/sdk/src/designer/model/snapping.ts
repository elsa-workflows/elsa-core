import type { InternalNode, Node } from "@xyflow/react";

export interface HelperLines {
  /** y in flow coordinates of a horizontal guide, with the x-range it spans. */
  horizontal?: { y: number; from: number; to: number };
  vertical?: { x: number; from: number; to: number };
}

export interface SnapResult {
  position: { x: number; y: number };
  lines: HelperLines;
}

const THRESHOLD = 6;

interface HandleY {
  kind: "in" | "out";
  /** y offset of the handle centre relative to the node's top. */
  offset: number;
}

/** Vertical offsets of the connection handles (ignores the hidden data-flow handles). */
function handleOffsets(node: InternalNode<Node>): HandleY[] {
  const bounds = node.internals.handleBounds;
  const list: HandleY[] = [];
  for (const h of bounds?.target ?? []) if (!h.id?.startsWith("__")) list.push({ kind: "in", offset: h.y + h.height / 2 });
  for (const h of bounds?.source ?? []) if (!h.id?.startsWith("__")) list.push({ kind: "out", offset: h.y + h.height / 2 });
  return list;
}

/**
 * Snaps a dragged node so its ports line up with neighbouring ports (straight connections) or its
 * edges line up with neighbouring nodes, and returns the guide lines to draw. Port alignment wins
 * over edge alignment because it is what makes connections straight.
 */
export function snapNode(dragged: InternalNode<Node>, position: { x: number; y: number }, others: InternalNode<Node>[]): SnapResult {
  const width = dragged.measured.width ?? 220;
  const height = dragged.measured.height ?? 60;
  const myHandles = handleOffsets(dragged);
  let best: { dy: number; y: number; otherX: number; otherW: number } | undefined;
  let bestX: { dx: number; x: number; otherY: number; otherH: number } | undefined;

  for (const other of others) {
    const ow = other.measured.width ?? 220;
    const oh = other.measured.height ?? 60;
    const ox = other.internals.positionAbsolute.x;
    const oy = other.internals.positionAbsolute.y;

    // Port-to-port: my in-port with their out-ports and vice versa.
    for (const mine of myHandles) {
      for (const theirs of handleOffsets(other)) {
        if (mine.kind === theirs.kind) continue;
        const dy = oy + theirs.offset - (position.y + mine.offset);
        if (Math.abs(dy) <= THRESHOLD && (!best || Math.abs(dy) < Math.abs(best.dy))) best = { dy, y: oy + theirs.offset, otherX: ox, otherW: ow };
      }
    }
    // Edge-to-edge (top / centre / bottom) when no port match.
    if (!best) {
      for (const [mineY, theirY] of [
        [position.y, oy],
        [position.y + height / 2, oy + oh / 2],
        [position.y + height, oy + oh],
      ]) {
        const dy = theirY - mineY;
        if (Math.abs(dy) <= THRESHOLD && (!best || Math.abs(dy) < Math.abs(best.dy))) best = { dy, y: theirY, otherX: ox, otherW: ow };
      }
    }
    for (const [mineX, theirX] of [
      [position.x, ox],
      [position.x + width / 2, ox + ow / 2],
      [position.x + width, ox + ow],
    ]) {
      const dx = theirX - mineX;
      if (Math.abs(dx) <= THRESHOLD && (!bestX || Math.abs(dx) < Math.abs(bestX.dx))) bestX = { dx, x: theirX, otherY: oy, otherH: oh };
    }
  }

  const snapped = { x: position.x + (bestX?.dx ?? 0), y: position.y + (best?.dy ?? 0) };
  const lines: HelperLines = {};
  if (best) lines.horizontal = { y: best.y, from: Math.min(snapped.x, best.otherX) - 24, to: Math.max(snapped.x + width, best.otherX + best.otherW) + 24 };
  if (bestX) lines.vertical = { x: bestX.x, from: Math.min(snapped.y, bestX.otherY) - 24, to: Math.max(snapped.y + height, bestX.otherY + bestX.otherH) + 24 };
  return { position: snapped, lines };
}
