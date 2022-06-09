import 'reflect-metadata';
import {Service} from "typedi";
import * as Icons from "../components/icons/activities";

export type ActivityType = string;
export type ActivityIcon = () => string;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIcon> = new Map<ActivityType, ActivityIcon>();

  constructor() {
    this.add('Elsa.WriteLine', Icons.WriteLineIcon);
    this.add('Elsa.ReadLine', Icons.ReadLineIcon);
    this.add('Elsa.If', Icons.IfIcon);
    this.add('Elsa.Flowchart', Icons.FlowchartIcon);
    this.add('Elsa.HttpEndpoint', Icons.HttpEndpointIcon);
    this.add('Elsa.ForEach', Icons.ForEachIcon);
    this.add('Elsa.Delay', Icons.DelayIcon);
    this.add('Elsa.Timer', Icons.TimerIcon);
    this.add('Elsa.Event', Icons.EventIcon);
    this.add('Elsa.RunJavaScript', Icons.RunJavaScriptIcon);
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
