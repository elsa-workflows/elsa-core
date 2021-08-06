import {Map} from '../utils/utils';
import {h} from "@stencil/core";
import {ReadLineIcon} from "../components/icons/read-line-icon";
import {WriteLineIcon} from "../components/icons/write-line-icon";
import {IfIcon} from "../components/icons/if-icon";
import {ForkIcon} from "../components/icons/fork-icon";
import {JoinIcon} from "../components/icons/join-icon";
import {TimerIcon} from "../components/icons/timer-icon";
import {SendEmailIcon} from "../components/icons/send-email-icon";
import {HttpEndpointIcon} from "../components/icons/http-endpoint-icon";
import {SendHttpRequestIcon} from "../components/icons/send-http-request-icon";
import {ScriptIcon} from "../components/icons/script-icon";
import {LoopIcon} from "../components/icons/loop-icon";
import {BreakIcon} from "../components/icons/break-icon";
import {SwitchIcon} from "../components/icons/switch-icon";
import {WriteHttpResponseIcon} from "../components/icons/write-http-response";
import {RedirectIcon} from "../components/icons/redirect-icon";
import {EraseIcon} from "../components/icons/erase-icon";
import {CogIcon} from "../components/icons/cog-icon";
import {RunWorkflowIcon} from "../components/icons/run-workflow-icon";
import {SendSignalIcon} from "../components/icons/send-signal-icon";
import {SignalReceivedIcon} from "../components/icons/signal-received-icon";
import {FinishIcon} from "../components/icons/finish-icon";
import {InterruptTriggerIcon} from "../components/icons/interrupt-trigger-icon";
import {CorrelateIcon} from "../components/icons/correlate-icon";
import {StateIcon} from "../components/icons/state-icon";
import {WebhookIcon} from "../components/icons/webhook-icon";

export class ActivityIconProvider {
  map: Map<() => any> = {
    'If': () => <IfIcon/>,
    'Fork': () => <ForkIcon/>,
    'Join': () => <JoinIcon/>,
    'For': () => <LoopIcon/>,
    'ForEach': () => <LoopIcon/>,
    'While': () => <LoopIcon/>,
    'ParallelForEach': () => <LoopIcon/>,
    'Break': () => <BreakIcon/>,
    'Switch': () => <SwitchIcon/>,
    'SetVariable': () => <CogIcon/>,
    'SetTransientVariable': () => <CogIcon/>,
    'SetContextId': () => <CogIcon/>,
    'Correlate': () => <CorrelateIcon/>,
    'SetName': () => <CogIcon/>,
    'RunWorkflow': () => <RunWorkflowIcon/>,
    'Timer': () => <TimerIcon/>,
    'StartAt': () => <TimerIcon/>,
    'Cron': () => <TimerIcon/>,
    'ClearTimer': () => <EraseIcon/>,
    'SendSignal': () => <SendSignalIcon/>,
    'SignalReceived': () => <SignalReceivedIcon/>,
    'Finish': () => <FinishIcon/>,
    'State': () => <StateIcon/>,
    'InterruptTrigger': () => <InterruptTriggerIcon/>,
    'RunJavaScript': () => <ScriptIcon/>,
    'ReadLine': () => <ReadLineIcon/>,
    'WriteLine': () => <WriteLineIcon/>,
    'HttpEndpoint': () => <HttpEndpointIcon/>,
    'SendHttpRequest': () => <SendHttpRequestIcon/>,
    'WriteHttpResponse': () => <WriteHttpResponseIcon/>,
    'Redirect': () => <RedirectIcon/>,
    'SendEmail': () => <SendEmailIcon/>,
    'Webhook': () => <WebhookIcon/>
  };

  register(activityType: string, icon: string) {
    this.map[activityType] = () => icon;
  }

  getIcon(activityType: string): any {
    const provider = this.map[activityType];

    if(!provider)
      return undefined;

    return provider();
  }
}

export const activityIconProvider = new ActivityIconProvider();
