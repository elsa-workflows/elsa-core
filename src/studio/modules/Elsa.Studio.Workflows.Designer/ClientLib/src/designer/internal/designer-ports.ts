export const designerPortMarkup = [
    {
        tagName: 'g',
        selector: 'port',
        children: [
            {
                tagName: 'circle',
                selector: 'hitArea',
                className: 'elsa-designer-port-hit-area',
            },
            {
                tagName: 'circle',
                selector: 'circle',
                className: 'elsa-designer-port-circle',
            },
        ],
    },
];

export function createDesignerPortGroups() {
    const createInteractionAttrs = () => ({
        port: {
            magnet: true,
        },
        hitArea: {
            r: 12,
            fill: 'transparent',
            stroke: 'transparent',
            pointerEvents: 'all',
            cursor: 'crosshair',
        },
    });

    return {
        in: {
            position: 'left',
            attrs: {
                ...createInteractionAttrs(),
                circle: {
                    r: 5,
                    stroke: 'var(--elsa-designer-port-stroke)',
                    strokeWidth: 2,
                    fill: 'var(--elsa-designer-port-surface)',
                    pointerEvents: 'none',
                },
                text: {
                    fontSize: 12,
                    fill: 'var(--elsa-designer-port-text)',
                },
            },
            label: {
                position: {
                    name: 'outside',
                },
            },
        },
        out: {
            position: 'right',
            attrs: {
                ...createInteractionAttrs(),
                circle: {
                    r: 5,
                    stroke: 'var(--elsa-designer-port-surface)',
                    strokeWidth: 2,
                    fill: 'var(--elsa-designer-port-stroke)',
                    pointerEvents: 'none',
                },
                text: {
                    fontSize: 12,
                    fill: 'var(--elsa-designer-port-text)',
                },
            },
            label: {
                position: {
                    name: 'outside',
                },
            },
        },
    };
}
