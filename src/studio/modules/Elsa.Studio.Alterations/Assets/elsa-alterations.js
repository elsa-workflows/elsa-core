// Tiny helpers used by the alterations designer host.
//
// The drop-target for activity-targeted alterations is detected purely from the DOM: the
// React Flow designer renders every activity inside `<div data-node-id="…">`, so we can use
// `document.elementFromPoint(...)` to find which node (if any) was under the cursor when the
// drag-end fired. This avoids any modification to the React Flow ClientLib.

export function findActivityAt(clientX, clientY) {
    if (typeof document === 'undefined') return null;
    const el = document.elementFromPoint(clientX, clientY);
    if (!el) return null;
    const host = el.closest('[data-node-id]');
    if (!host) return null;
    const id = host.getAttribute('data-node-id');
    if (!id) return null;
    // The React Flow designer sets `data-node-id` to the React Flow node id, which is the
    // activity's plain id (set in Designer.tsx via `id: node.id`). Use it as-is.
    return id;
}

export function getActivityDisplayName(activityId) {
    if (!activityId || typeof document === 'undefined') return null;
    const host = document.querySelector(`[data-node-id="${CSS.escape(activityId)}"]`);
    if (!host) return activityId;
    const label = host.querySelector('.activity-label');
    const text = label?.textContent?.replace(/\s+/g, ' ').trim();
    return text && text.length > 0 ? text : activityId;
}
