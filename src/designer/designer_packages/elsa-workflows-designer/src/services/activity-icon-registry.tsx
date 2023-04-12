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
  HttpEndpointIcon, HttpResponseIcon, CorrelateIcon
} from "../components/icons/activities";
import {ForIcon} from "../components/icons/activities/for";
import {FinishIcon} from "../components/icons/activities/finish";
import {FaultIcon} from "../components/icons/activities/fault";
import {SetNameIcon} from "../components/icons/activities/set-name";
import {SetVariableIcon} from "../components/icons/activities/set-variable";
import {StartIcon} from "../components/icons/activities/start";
import {StartAtIcon} from "../components/icons/activities/startat";
import {BreakIcon} from "../components/icons/activities/break";
import {ForkIcon} from "../components/icons/activities/fork";
import {CompleteIcon} from "../components/icons/activities/complete";
import {PublishEventIcon} from "../components/icons/activities/publish-event";
import {RunTaskIcon} from "../components/icons/activities/run-task";
import {CronIcon} from "../components/icons/activities/cron";

export type ActivityType = string;
export type ActivityIconProducer = (ActivityIconSettings?) => any;

// A registry of activity icons.
@Service()
export class ActivityIconRegistry {
  private iconMap: Map<ActivityType, ActivityIconProducer> = new Map<ActivityType, ActivityIconProducer>();

  constructor() {
    this.add('Elsa.WriteLine', settings => <WriteLineIcon size={settings?.size}/>);
    this.add('Elsa.ReadLine', settings => <ReadLineIcon size={settings?.size}/>);
    this.add('Elsa.If', settings => <IfIcon size={settings?.size}/>);
    this.add('Elsa.Flowchart', settings => <FlowchartIcon size={settings?.size}/>);
    this.add('Elsa.Fork', settings => <ForkIcon size={settings?.size}/>);
    this.add('Elsa.HttpEndpoint', settings => <HttpEndpointIcon size={settings?.size}/>);
    this.add('Elsa.WriteHttpResponse', settings => <HttpResponseIcon size={settings?.size}/>);
    this.add('Elsa.ForEach', settings => <ForEachIcon size={settings?.size}/>);
    this.add('Elsa.For', settings => <ForIcon size={settings?.size}/>);
    this.add('Elsa.While', settings => <ForIcon size={settings?.size}/>);
    this.add('Elsa.Break', settings => <BreakIcon size={settings?.size}/>);
    this.add('Elsa.Delay', settings => <DelayIcon size={settings?.size}/>);
    this.add('Elsa.Timer', settings => <TimerIcon size={settings?.size}/>);
    this.add('Elsa.Cron', settings => <CronIcon size={settings?.size}/>);
    this.add('Elsa.StartAt', settings => <StartAtIcon size={settings?.size}/>);
    this.add('Elsa.FlowDecision', settings => <FlowDecisionIcon size={settings?.size}/>);
    this.add('Elsa.Event', settings => <EventIcon size={settings?.size}/>);
    this.add('Elsa.PublishEvent', settings => <PublishEventIcon size={settings?.size}/>);
    this.add('Elsa.RunJavaScript', settings => <RunJavaScriptIcon size={settings?.size}/>);
    this.add('Elsa.FlowJoin', settings => <FlowJoinIcon size={settings?.size}/>);
    this.add('Elsa.FlowNode', settings => <FlowNodeIcon size={settings?.size}/>);
    this.add("Elsa.Correlate", settings => <CorrelateIcon size={settings?.size}/>)
    this.add("Elsa.Start", settings => <StartIcon size={settings?.size}/>)
    this.add("Elsa.Finish", settings => <FinishIcon size={settings?.size}/>)
    this.add('Elsa.Fault', settings => <FaultIcon size={settings?.size}/>);
    this.add('Elsa.Complete', settings => <CompleteIcon size={settings?.size}/>);
    this.add('Elsa.SetName', settings => <SetNameIcon size={settings?.size}/>);
    this.add('Elsa.SetVariable', settings => <SetVariableIcon size={settings?.size}/>);
    this.add('Elsa.RunTask', settings => <RunTaskIcon size={settings?.size}/>);
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
