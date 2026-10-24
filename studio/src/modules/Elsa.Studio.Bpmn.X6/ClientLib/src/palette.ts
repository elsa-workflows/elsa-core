/**
 * The palette: a toolbar of the elements the designer creates, in the order and with the names the bpmn-js designer's
 * palette uses, then a text annotation, which bpmn-js adds from its context pad. It only draws the buttons; the designer
 * handles dragging and placing what they create.
 */

interface PaletteEntry {
    readonly type: string;
    readonly title: string;
    /** SVG content in a 24x24 view box, stroked with the current colour. */
    readonly icon: string;
}

const ENTRIES: readonly PaletteEntry[] = [
    { type: 'bpmn:StartEvent', title: 'Create start event', icon: '<circle cx="12" cy="12" r="8" stroke-width="1.5" />' },
    {
        type: 'bpmn:IntermediateThrowEvent',
        title: 'Create intermediate/boundary event',
        icon: '<circle cx="12" cy="12" r="8" stroke-width="1.5" /><circle cx="12" cy="12" r="5.5" stroke-width="1.5" />'
    },
    { type: 'bpmn:EndEvent', title: 'Create end event', icon: '<circle cx="12" cy="12" r="8" stroke-width="3" />' },
    { type: 'bpmn:ExclusiveGateway', title: 'Create gateway', icon: '<path d="M12 3 L21 12 L12 21 L3 12 Z M9 9 L15 15 M15 9 L9 15" stroke-width="1.5" />' },
    { type: 'bpmn:Task', title: 'Create task', icon: '<rect x="3" y="6" width="18" height="12" rx="3" stroke-width="1.5" />' },
    { type: 'bpmn:DataObjectReference', title: 'Create data object reference', icon: '<path d="M6 3 L14 3 L18 7 L18 21 L6 21 Z M14 3 L14 7 L18 7" stroke-width="1.5" />' },
    {
        type: 'bpmn:DataStoreReference',
        title: 'Create data store reference',
        icon: '<path d="M4 6 L4 18 A8 3 0 0 0 20 18 L20 6 M4 6 A8 3 0 0 0 20 6 A8 3 0 0 0 4 6 M4 9 A8 3 0 0 0 20 9" stroke-width="1.5" />'
    },
    { type: 'bpmn:TextAnnotation', title: 'Create text annotation', icon: '<path d="M10 4 L5 4 L5 20 L10 20 M9 9 L19 9 M9 13 L19 13" stroke-width="1.5" />' }
];

/** Builds the palette. Each button carries the element type it creates in `data-type`. */
export function createPalette(): HTMLElement {
    const palette = document.createElement('div');
    palette.className = 'elsa-x6-palette';
    palette.setAttribute('role', 'toolbar');
    palette.setAttribute('aria-label', 'Create');
    palette.setAttribute('aria-orientation', 'vertical');
    for (const entry of ENTRIES) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'elsa-x6-palette-entry';
        button.title = entry.title;
        button.setAttribute('aria-label', entry.title);
        button.dataset.type = entry.type;
        button.innerHTML = `<svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" aria-hidden="true">${entry.icon}</svg>`;
        palette.append(button);
    }
    return palette;
}
