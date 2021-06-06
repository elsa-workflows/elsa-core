import {ElsaPlugin} from "./elsa-plugin";
import {IfPlugin} from "../plugins/if-plugin";
import {HttpEndpointPlugin} from "../plugins/http-endpoint-plugin";
import {TimerPlugin} from "../plugins/timer-plugin";
import {WriteLinePlugin} from "../plugins/write-line-plugin";
import {SendEmailPlugin} from "../plugins/send-email-plugin";
import {DefaultDriversPlugin} from "../plugins/default-drivers-plugin";
import {ForkPlugin} from "../plugins/fork-plugin";
import {RunJavascriptPlugin} from "../plugins/run-javascript-plugin";
import {ActivityIconProviderPlugin} from "../plugins/activity-icon-provider-plugin";
import {SwitchPlugin} from "../plugins/switch-plugin";
import {WhilePlugin} from "../plugins/while-plugin";
import {StartAtPlugin} from "../plugins/start-at-plugin";
import {CronPlugin} from "../plugins/cron-plugin";
import {SignalReceivedPlugin} from "../plugins/signal-received-plugin";
import {SendSignalPlugin} from "../plugins/send-signal-plugin";
import {UserTaskPlugin} from "../plugins/user-task-plugin";
import {StatePlugin} from "../plugins/state-plugin";
import {SendHttpRequestPlugin} from "../plugins/send-http-request-plugin";

export class PluginManager {

  plugins: ElsaPlugin = [];

  constructor() {
    this.plugins = [
      new DefaultDriversPlugin(),
      new ActivityIconProviderPlugin(),
      new IfPlugin(),
      new WhilePlugin(),
      new ForkPlugin(),
      new SwitchPlugin(),
      new HttpEndpointPlugin(),
      new SendHttpRequestPlugin(),
      new TimerPlugin(),
      new StartAtPlugin(),
      new CronPlugin(),
      new SignalReceivedPlugin(),
      new SendSignalPlugin(),
      new WriteLinePlugin(),
      new StatePlugin(),
      new RunJavascriptPlugin(),
      new UserTaskPlugin(),
      new SendEmailPlugin()
    ];
  }

  initialize() {
  }
}

export const pluginManager = new PluginManager();
