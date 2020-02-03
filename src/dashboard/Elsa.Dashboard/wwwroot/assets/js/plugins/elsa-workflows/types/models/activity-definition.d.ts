import { ActivityPropertyDescriptor } from "./";
export declare type Lambda<T = any> = string | T;
export interface ActivityDefinition {
    type: string;
    displayName: string;
    description: string;
    runtimeDescription?: Lambda<string>;
    category: string;
    icon?: string;
    properties: Array<ActivityPropertyDescriptor>;
    outcomes?: Lambda<Array<string>>;
}
