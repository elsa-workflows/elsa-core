import {Connection} from "./models";
import {Activity, ActivityDescriptor} from "../../../models";
import {ActivityNode} from "./activity-walker";

export interface TransposeHandler {
  transpose: (context: TransposeContext) => boolean;
  untranspose: (context: UntransposeContext) => Array<UntransposedConnection>;
}

export interface TransposeContext {
    connection: Connection;
    source: Activity;
    target: Activity;
    sourceDescriptor: ActivityDescriptor;
    targetDescriptor: ActivityDescriptor;
}

export interface UntransposeContext {
  //activityNode: ActivityNode;
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
}

export interface UntransposedConnection {
  source: Activity;
  sourcePort: string;
  target: Activity;
  targetPort: string;
}
