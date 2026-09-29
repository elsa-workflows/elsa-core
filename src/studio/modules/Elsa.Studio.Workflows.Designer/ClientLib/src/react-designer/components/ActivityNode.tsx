import { useEffect, useRef, memo, Fragment, useCallback, type MouseEvent as ReactMouseEvent } from 'react';
import { Handle, Position, useNodeId, type NodeProps } from '@xyflow/react';
import type { ElsaActivity, ElsaActivityStats, ElsaPort } from '../types';

export interface ActivityNodeData {
    activity: ElsaActivity;
    ports: ElsaPort[];
    activityStats?: ElsaActivityStats;
    selectedPort?: string | null;
    readOnly?: boolean;
    onEmbeddedPortClick?: (activity: ElsaActivity, portName: string) => void;
    onDeleteRequest?: (nodeId: string) => void;
    [key: string]: unknown;
}

const activityTagName = 'elsa-activity-wrapper';

// The Blazor custom element receives stats as a property (cannot serialize
// through an HTML attribute). Treat it as a plain HTMLElement with one extra
// field so the rest of the code stays type-safe.
interface ActivityWrapperElement extends HTMLElement {
    stats: ElsaActivityStats | null | undefined;
}

// The activity body itself is a Blazor custom element registered via
// RegisterForJavaScript("elsa-activity-wrapper"). We just create the tag
// and set the same attributes the X6 path sets in create-activity-element.ts;
// Blazor hydrates it on insertion.
function ActivityNodeImpl({ id, data, selected }: NodeProps) {
    const hostRef = useRef<HTMLDivElement | null>(null);
    const elementRef = useRef<ActivityWrapperElement | null>(null);
    const nodeId = useNodeId();
    const { activity, ports, activityStats, selectedPort, readOnly, onEmbeddedPortClick, onDeleteRequest } =
        data as unknown as ActivityNodeData;
    const elementId = `activity-${id}`;

    const onDeleteClick = useCallback((e: ReactMouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        e.preventDefault();
        onDeleteRequest?.(id);
    }, [id, onDeleteRequest]);

    useEffect(() => {
        const host = hostRef.current;
        if (!host) return;
        const el = document.createElement(activityTagName) as ActivityWrapperElement;
        el.id = elementId;
        el.setAttribute('element-id', elementId);
        el.setAttribute('activity-id', id);
        el.setAttribute('activity-json', JSON.stringify(activity));
        if (selectedPort) el.setAttribute('selected-port-name', selectedPort);
        if (activityStats) el.stats = activityStats;
        host.appendChild(el);
        elementRef.current = el;
        return () => {
            if (elementRef.current && elementRef.current.parentNode === host) {
                host.removeChild(elementRef.current);
            }
            elementRef.current = null;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id]);

    useEffect(() => {
        const el = elementRef.current as ActivityWrapperElement | null;
        if (!el) return;
        // Avoid retriggering a Blazor custom-element re-hydration when the
        // activity reference changed but the JSON content didn't — otherwise
        // every parent re-render flickers the node and can swallow the first
        // click that prompted the re-render.
        const json = JSON.stringify(activity);
        if (el.getAttribute('activity-json') !== json) {
            el.setAttribute('activity-json', json);
        }
        const currentSelectedPort = el.getAttribute('selected-port-name');
        if (selectedPort) {
            if (currentSelectedPort !== selectedPort) el.setAttribute('selected-port-name', selectedPort);
        } else if (currentSelectedPort !== null) {
            el.removeAttribute('selected-port-name');
        }
        if (el.stats !== activityStats) el.stats = activityStats ?? null;
    }, [activity, selectedPort, activityStats]);

    // Delegated click listener for embedded ports rendered by ActivityWrapper
    // (V1/V2). When a user clicks the ".embedded-port" body inside a composite
    // activity (e.g. Sequence's Body), we navigate into it. The .embedded-port
    // div carries the port name in data-port-name. We stop propagation so
    // React Flow's onNodeClick doesn't also fire.
    useEffect(() => {
        const host = hostRef.current;
        if (!host) return;
        const handler = (event: MouseEvent) => {
            const target = event.target as HTMLElement | null;
            if (!target) return;
            const portEl = target.closest<HTMLElement>('.embedded-port');
            if (!portEl) return;
            const portName = portEl.getAttribute('data-port-name');
            if (!portName) return;
            event.stopPropagation();
            onEmbeddedPortClick?.(activity, portName);
        };
        host.addEventListener('click', handler);
        return () => host.removeEventListener('click', handler);
    }, [activity, onEmbeddedPortClick]);

    const inPorts = ports.filter(p => p.group === 'in');
    const outPorts = ports.filter(p => p.group === 'out');

    // The display label X6 paints next to the port circle. Falls back to the
    // port id (which is also the technical name) so something is always shown.
    const portLabel = (p: ElsaPort) => p.attrs?.text?.text?.trim() || p.id;

    const renderInPorts = inPorts.length > 0 ? inPorts : [{ id: 'in', group: 'in' } as ElsaPort];
    const renderOutPorts = outPorts.length > 0 ? outPorts : [{ id: 'out', group: 'out' } as ElsaPort];

    return (
        // Don't pin width/height — the React Flow node wrapper auto-sizes to
        // this container, which auto-sizes to the activity body's natural
        // size. That keeps the click target flush with the visible card.
        <div ref={hostRef} className="elsa-react-activity-host" data-node-id={nodeId}>
            {selected && !readOnly && onDeleteRequest && (
                <button
                    type="button"
                    className="elsa-react-flow-node-delete nodrag nopan"
                    onClick={onDeleteClick}
                    onMouseDown={(e) => e.stopPropagation()}
                    title="Delete activity"
                    aria-label="Delete activity"
                >
                    ×
                </button>
            )}
            {renderInPorts.map((p, i, arr) => {
                const top = `${((i + 1) / (arr.length + 1)) * 100}%`;
                return (
                    <Fragment key={`in-${p.id}`}>
                        <Handle id={p.id} type="target" position={Position.Left} style={{ top }} />
                        {/* Show the label only when the port carries a meaningful name beyond the default "in" placeholder. */}
                        {(inPorts.length > 0) && (
                            <span className="elsa-react-flow-port-label elsa-react-flow-port-label-in" style={{ top }}>
                                {portLabel(p)}
                            </span>
                        )}
                    </Fragment>
                );
            })}
            {renderOutPorts.map((p, i, arr) => {
                const top = `${((i + 1) / (arr.length + 1)) * 100}%`;
                return (
                    <Fragment key={`out-${p.id}`}>
                        <Handle id={p.id} type="source" position={Position.Right} style={{ top }} />
                        {/* Always show output labels — they identify branches like Pass/Fail/Done. */}
                        <span className="elsa-react-flow-port-label elsa-react-flow-port-label-out" style={{ top }}>
                            {portLabel(p)}
                        </span>
                    </Fragment>
                );
            })}
        </div>
    );
}

export const ActivityNode = memo(ActivityNodeImpl);
