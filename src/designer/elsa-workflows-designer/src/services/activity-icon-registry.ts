import {Service} from "typedi";
import * as Icons from "../components/icons/activities";

export type ActivityType = string;
export type ActivityIcon = () => string;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIcon> = new Map<ActivityType, ActivityIcon>();

  constructor() {
    this.add('Console.WriteLine', Icons.WriteLineIcon);
    this.add('Console.ReadLine', Icons.ReadLineIcon);
    this.add('ControlFlow.If', Icons.IfIcon);
    this.add('Workflows.Flowchart', Icons.FlowchartIcon);
    this.add('Http.HttpTrigger', Icons.HttpTriggerIcon);
  }

  public add(activityType: ActivityType, icon: ActivityIcon) {
    this.iconMap.set(activityType, icon);
  }

  public get(activityType: ActivityType): ActivityIcon {
    return this.iconMap.get(activityType);
  }

  has(activityType: string): boolean {
    return this.iconMap.has(activityType);
  }
}
