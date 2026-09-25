import { useEffect, useMemo, useRef, useState } from 'react';
import type { ActivityDescriptorDto } from '../types';

export interface ConnectMenuProps {
    /** Screen-coordinate anchor (where the user released the connection). */
    clientX: number;
    clientY: number;
    /** Pre-loaded catalog of activities. */
    activities: ActivityDescriptorDto[];
    /** True while the catalog is still being fetched. */
    loading?: boolean;
    onPick: (descriptor: ActivityDescriptorDto) => void;
    onClose: () => void;
}

/**
 * Floating activity picker that appears when the user drags from a port and
 * releases on empty canvas. Mirrors the existing toolbox in spirit (searchable,
 * grouped by category) but is a tight, in-place popup.
 */
export function ConnectMenu({ clientX, clientY, activities, loading, onPick, onClose }: ConnectMenuProps) {
    const [search, setSearch] = useState('');
    const [activeIndex, setActiveIndex] = useState(0);
    const containerRef = useRef<HTMLDivElement | null>(null);
    const inputRef = useRef<HTMLInputElement | null>(null);

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase();
        if (!q) return activities;
        return activities.filter(a =>
            a.displayName.toLowerCase().includes(q) ||
            (a.category ?? '').toLowerCase().includes(q) ||
            a.typeName.toLowerCase().includes(q),
        );
    }, [activities, search]);

    // Reset highlighted item when the filter changes.
    useEffect(() => { setActiveIndex(0); }, [search, activities.length]);

    // Auto-focus the search box on open.
    useEffect(() => {
        inputRef.current?.focus();
    }, []);

    // Close on outside click / Escape.
    useEffect(() => {
        const onMouseDown = (e: MouseEvent) => {
            if (!containerRef.current) return;
            if (!containerRef.current.contains(e.target as Node)) onClose();
        };
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') {
                e.stopPropagation();
                onClose();
            }
        };
        // mousedown (not click) so React Flow can't open then immediately close
        // via its own click-on-pane handler.
        document.addEventListener('mousedown', onMouseDown, true);
        document.addEventListener('keydown', onKeyDown);
        return () => {
            document.removeEventListener('mousedown', onMouseDown, true);
            document.removeEventListener('keydown', onKeyDown);
        };
    }, [onClose]);

    const onInputKey = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setActiveIndex(i => Math.min(i + 1, filtered.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setActiveIndex(i => Math.max(i - 1, 0));
        } else if (e.key === 'Enter') {
            e.preventDefault();
            const pick = filtered[activeIndex];
            if (pick) onPick(pick);
        }
    };

    // Position: offset slightly down/right of the cursor so the menu doesn't
    // sit directly under the pointer when the user releases.
    const style: React.CSSProperties = {
        position: 'fixed',
        left: clientX + 4,
        top: clientY + 4,
        zIndex: 1000,
    };

    return (
        <div ref={containerRef} className="elsa-react-flow-connect-menu" style={style} onMouseDown={e => e.stopPropagation()}>
            <input
                ref={inputRef}
                className="elsa-react-flow-connect-menu-search"
                type="text"
                placeholder="Search activities…"
                value={search}
                onChange={e => setSearch(e.target.value)}
                onKeyDown={onInputKey}
            />
            <ul className="elsa-react-flow-connect-menu-list">
                {loading && <li className="elsa-react-flow-connect-menu-empty">Loading…</li>}
                {!loading && filtered.length === 0 && (
                    <li className="elsa-react-flow-connect-menu-empty">No matches</li>
                )}
                {filtered.map((a, i) => (
                    <li
                        key={`${a.typeName}/${a.version}`}
                        className={`elsa-react-flow-connect-menu-item${i === activeIndex ? ' is-active' : ''}`}
                        onMouseEnter={() => setActiveIndex(i)}
                        onClick={() => onPick(a)}
                    >
                        <span className="elsa-react-flow-connect-menu-swatch" style={{ background: a.color }} />
                        <span className="elsa-react-flow-connect-menu-text">
                            <span className="elsa-react-flow-connect-menu-name">{a.displayName}</span>
                            {a.category && <span className="elsa-react-flow-connect-menu-cat">{a.category}</span>}
                        </span>
                    </li>
                ))}
            </ul>
        </div>
    );
}
