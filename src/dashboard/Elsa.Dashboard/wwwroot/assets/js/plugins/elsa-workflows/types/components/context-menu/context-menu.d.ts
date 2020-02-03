import { EventEmitter } from '../../stencil.core';
import { Point } from "../../models";
export declare class ContextMenu {
    el: HTMLElement;
    target: HTMLElement | ShadowRoot;
    targetSelector: string;
    isHidden: boolean;
    position: Point;
    targetChangeHandler(newValue: HTMLElement, oldValue: HTMLElement): void;
    targetSelectorChangeHandler(newValue: string): void;
    handleBodyClick(): void;
    handleContextMenu(): void;
    contextMenuEvent: EventEmitter;
    handleContextMenuEvent(e: MouseEvent): Promise<void>;
    componentDidLoad(): void;
    render(): any;
    private setupTarget;
    private onContextMenu;
    private onContextMenuClick;
}
