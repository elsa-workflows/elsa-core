import { Activity } from "../../models";
import { ActivityExecutionEventBlock } from "./models";

export interface JournalItemSelectedArgs {
    activity: Activity;
    executionLog: ActivityExecutionEventBlock;
}