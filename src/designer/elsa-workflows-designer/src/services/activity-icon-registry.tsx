import {h} from "@stencil/core";
import 'reflect-metadata';
import {Service} from "typedi";

import {
  DefaultIcon,
  DelayIcon, TimerIcon,
  EventIcon,
  FlowchartIcon, FlowDecisionIcon, FlowJoinIcon, FlowNodeIcon,
  ForEachIcon,
  IfIcon,
  ReadLineIcon, WriteLineIcon,
  RunJavaScriptIcon,
  HttpEndpointIcon, HttpResponseIcon, HttpRequestIcon
} from "../components/icons/activities";

export type ActivityType = string;
export type ActivityIconProducer = (ActivityIconSettings?) => any;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIconProducer> = new Map<ActivityType, ActivityIconProducer>();

  constructor() {
    this.add('Elsa.WriteLine', settings => <WriteLineIcon size={settings?.size}/>);
    this.add('Elsa.WriteLine', settings => <WriteLineIcon size={settings?.size}/>);
    this.add('Elsa.ReadLine', settings => <ReadLineIcon size={settings?.size}/>);
    this.add('Elsa.If', settings => <IfIcon size={settings?.size}/>);
    this.add('Elsa.Flowchart', settings => <FlowchartIcon size={settings?.size}/>);
    this.add('Elsa.HttpEndpoint', settings => <HttpEndpointIcon size={settings?.size}/>);
    this.add('Elsa.WriteHttpResponse', settings => <HttpResponseIcon size={settings?.size}/>);
    this.add('Elsa.ForEach', settings => <ForEachIcon size={settings?.size}/>);
    this.add('Elsa.Delay', settings => <DelayIcon size={settings?.size}/>);
    this.add('Elsa.Timer', settings => <TimerIcon size={settings?.size}/>);
    this.add('Elsa.FlowDecision', settings => <FlowDecisionIcon size={settings?.size}/>);
    this.add('Elsa.Event', settings => <EventIcon size={settings?.size}/>);
    this.add('Elsa.RunJavaScript', settings => <RunJavaScriptIcon size={settings?.size}/>);
    this.add('Elsa.FlowJoin', settings => <FlowJoinIcon size={settings?.size}/>);
    this.add('Elsa.FlowNode', settings => <FlowNodeIcon size={settings?.size}/>);
    this.add('Elsa.SendHttpRequest', settings => <HttpRequestIcon size={settings?.size}/>);
  }

  public add(activityType: ActivityType, icon: ActivityIconProducer) {
    this.iconMap.set(activityType, icon);
  }

  public get(activityType: ActivityType): ActivityIconProducer {
    return this.iconMap.get(activityType);
  }

  public getOrDefault(activityType: ActivityType): ActivityIconProducer {
    return this.iconMap.get(activityType) ?? ((settings) => <DefaultIcon size={settings?.size}/>);
  }

  has(activityType: string): boolean {
    return this.iconMap.has(activityType);
  }
}
