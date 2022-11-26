import {Activity, ActivityDescriptor} from "../models";
import {ActivityNameFormatter, ActivityNode} from "../services";
import {Container} from "typedi";

const activityNameFormatter = Container.get(ActivityNameFormatter);

export async function generateUniqueActivityName(activities: Array<Activity>, activityDescriptor: ActivityDescriptor): Promise<string> {
  const activityType = activityDescriptor.typeName;
  const activityCount = activities.filter(x => x.type == activityType).length;
  let counter = activityCount + 1;
  let newName = activityNameFormatter.format({activityDescriptor, count: counter, activities});

  while (!!activities.find(x => x.id == newName))
    newName = activityNameFormatter.format({activityDescriptor, count: ++counter, activities});

  return newName;
}
