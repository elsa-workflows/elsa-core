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
export type ActivityIcon = (ActivityIconSettings?) => any;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIcon> = new Map<ActivityType, ActivityIcon>();

  constructor() {
    this.add('Elsa.WriteLine', settings => <WriteLineIcon size={settings?.size}/>);
    this.add('Elsa.ReadLine', settings => <ReadLineIcon size={settings?.size}/>);
    this.add('Elsa.If', settings => <IfIcon size={settings?.size}/>);
    this.add('Elsa.Flowchart', settings => <FlowchartIcon size={settings?.size}/>);
    this.add('Elsa.HttpEndpoint', settings => <HttpEndpointIcon size={settings?.size}/>);
    this.add('Elsa.ForEach', settings => <ForEachIcon size={settings?.size}/>);
    this.add('Elsa.Delay', settings => <DelayIcon size={settings?.size}/>);
    this.add('Elsa.Timer', settings => <TimerIcon size={settings?.size}/>);
    this.add('Elsa.Event', settings => <EventIcon size={settings?.size}/>);
    this.add('Elsa.RunJavaScript', settings => <RunJavaScriptIcon size={settings?.size}/>);
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
