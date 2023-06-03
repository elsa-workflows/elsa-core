import { Activity } from "../../models";
import { ActivityNode } from "../../services";
import { ActivityExecutionEventBlock } from "./models";

export interface JournalItemSelectedArgs {
    activity: Activity;
    executionEventBlock: ActivityExecutionEventBlock;
    activityNode: ActivityNode;
}
