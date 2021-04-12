import {Map} from "../../../../utils/utils";

export interface SwitchCase {
    name: string;
    expressions?: Map<string>;
    syntax?: string;
}