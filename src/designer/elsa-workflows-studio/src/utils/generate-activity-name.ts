import {ActivityDescriptor} from "../models";
import {ActivityNameFormatter, ActivityNode} from "../services";
import {Container} from "typedi";

const activityNameFormatter = Container.get(ActivityNameFormatter);

export async function generateUniqueActivityName(activityNodes: Array<ActivityNode>, activityDescriptor: ActivityDescriptor): Promise<string> {
  const activityType = activityDescriptor.type;
  const activityCount = activityNodes.filter(x => x.activity.type == activityType).length;
  let counter = activityCount + 1;
  let newName = activityNameFormatter.format({activityDescriptor, count: counter, activityNodes});

  while (!!activityNodes.find(x => x.activity.activityId == newName))
    newName = activityNameFormatter.format({activityDescriptor, count: ++counter, activityNodes});

  return newName;
}
