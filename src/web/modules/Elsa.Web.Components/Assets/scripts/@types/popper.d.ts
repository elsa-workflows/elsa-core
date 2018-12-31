declare namespace Popper {
    type Position = 'top' | 'right' | 'bottom' | 'left';

    type Placement = 'auto-start'
        | 'auto'
        | 'auto-end'
        | 'top-start'
        | 'top'
        | 'top-end'
        | 'right-start'
        | 'right'
        | 'right-end'
        | 'bottom-end'
        | 'bottom'
        | 'bottom-start'
        | 'left-end'
        | 'left'
        | 'left-start';

    type Boundary = 'scrollParent' | 'viewport' | 'window';

    type ModifierFn = (data: Data, options: Object) => Data;

    interface BaseModifier {
        order?: number;
        enabled?: boolean;
        fn?: ModifierFn;
    }

    interface Modifiers {
        shift?: BaseModifier;
        offset?: BaseModifier & {
            offset?: number | string,
        };
        preventOverflow?: BaseModifier & {
            priority?: Position[],
            padding?: number,
            boundariesElement?: Boundary | Element,
            escapeWithReference?: boolean
        };
        keepTogether?: BaseModifier;
        arrow?: BaseModifier & {
            element?: string | Element,
        };
        flip?: BaseModifier & {
            behavior?: 'flip' | 'clockwise' | 'counterclockwise' | Position[],
            padding?: number,
            boundariesElement?: Boundary | Element,
        };
        inner?: BaseModifier;
        hide?: BaseModifier;
        applyStyle?: BaseModifier & {
            onLoad?: Function,
            gpuAcceleration?: boolean,
        };
        computeStyle?: BaseModifier & {
            gpuAcceleration?: boolean;
            x?: 'bottom' | 'top',
            y?: 'left' | 'right'
        };

        [name: string]: (BaseModifier & Record<string, any>) | undefined;
    }

    interface Offset {
        top: number;
        left: number;
        width: number;
        height: number;
    }

    interface Data {
        instance: Popper;
        placement: Placement;
        originalPlacement: Placement;
        flipped: boolean;
        hide: boolean;
        arrowElement: Element;
        styles: CSSStyleDeclaration;
        boundaries: Object;
        offsets: {
            popper: Offset,
            reference: Offset,
            arrow: {
                top: number,
                left: number,
            },
        };
    }

    interface PopperOptions {
        placement?: Placement;
        eventsEnabled?: boolean;
        modifiers?: Modifiers;
        removeOnDestroy?: boolean;

        onCreate?(data: Data): void;

        onUpdate?(data: Data): void;
    }

    interface ReferenceObject {
        clientHeight: number;
        clientWidth: number;

        getBoundingClientRect(): ClientRect;
    }
}

declare class Popper {
    static modifiers: (Popper.BaseModifier & { name: string })[];
    static placements: Popper.Placement[];
    static Defaults: Popper.PopperOptions;

    options: Popper.PopperOptions;

    constructor(reference: Element | Popper.ReferenceObject, popper: Element, options?: Popper.PopperOptions);

    destroy(): void;

    update(): void;

    scheduleUpdate(): void;

    enableEventListeners(): void;

    disableEventListeners(): void;
}