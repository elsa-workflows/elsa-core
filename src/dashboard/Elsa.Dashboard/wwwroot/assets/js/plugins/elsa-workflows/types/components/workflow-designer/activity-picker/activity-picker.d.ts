import { EventEmitter } from '../../../stencil.core';
import { ActivityDefinition } from '../../../models';
export declare class ActivityPicker {
    el: HTMLElement;
    activityDefinitions: Array<ActivityDefinition>;
    isVisible: boolean;
    filterText: string;
    selectedCategory: string;
    show(): Promise<void>;
    hide(): Promise<void>;
    activitySelected: EventEmitter;
    private modal;
    private onActivitySelected;
    private onFilterTextChanged;
    private selectCategory;
    render(): any;
    renderActivity: (activity: ActivityDefinition) => any;
}
