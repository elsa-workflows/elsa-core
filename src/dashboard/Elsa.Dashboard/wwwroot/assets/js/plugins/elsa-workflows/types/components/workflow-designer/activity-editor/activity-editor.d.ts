import { EventEmitter } from '../../../stencil.core';
import { Activity, ActivityDefinition } from "../../../models";
export declare class ActivityEditor {
    el: HTMLElement;
    activityDefinitions: Array<ActivityDefinition>;
    activity: Activity;
    show: boolean;
    submit: EventEmitter;
    modal: HTMLElement;
    renderer: HTMLWfActivityRendererElement;
    onSubmit(e: Event): Promise<void>;
    componentDidRender(): void;
    render(): any;
}
