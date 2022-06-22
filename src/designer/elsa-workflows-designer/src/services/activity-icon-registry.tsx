import {h} from "@stencil/core";
import 'reflect-metadata';
import {Service} from "typedi";

import {
  DelayIcon,
  EventIcon,
  FlowchartIcon,
  ForEachIcon,
  HttpEndpointIcon,
  IfIcon,
  ReadLineIcon,
  RunJavaScriptIcon,
  TimerIcon,
  WriteLineIcon
} from "../components/icons/activities";

export type ActivityType = string;
export type ActivityIcon = () => any;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIcon> = new Map<ActivityType, ActivityIcon>();

  constructor() {
    this.add('Elsa.WriteLine', () => <WriteLineIcon/>);
    this.add('Elsa.ReadLine', () => <ReadLineIcon/>);
    this.add('Elsa.If', () => <IfIcon/>);
    this.add('Elsa.Flowchart', () => <FlowchartIcon/>);
    this.add('Elsa.HttpEndpoint', () => <HttpEndpointIcon/>);
    this.add('Elsa.ForEach', () => <ForEachIcon/>);
    this.add('Elsa.Delay', () => <DelayIcon/>);
    this.add('Elsa.Timer', () => <TimerIcon/>);
    this.add('Elsa.Event', () => <EventIcon/>);
    this.add('Elsa.RunJavaScript', () => <RunJavaScriptIcon/>);
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
