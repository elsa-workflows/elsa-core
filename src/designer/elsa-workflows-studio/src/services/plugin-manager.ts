import {ElsaPlugin} from "./elsa-plugin";
import {IfPlugin} from "../plugins/if-plugin";
import {HttpEndpointPlugin} from "../plugins/http-endpoint-plugin";
import {TimerPlugin} from "../plugins/timer-plugin";
import {WriteLinePlugin} from "../plugins/write-line-plugin";
import {SendEmailPlugin} from "../plugins/send-email-plugin";
import {DefaultDriversPlugin} from "../plugins/default-drivers-plugin";
import {ActivityIconProviderPlugin} from "../plugins/activity-icon-provider-plugin";
import {SwitchPlugin} from "../plugins/switch-plugin";
import {WhilePlugin} from "../plugins/while-plugin";
import {StartAtPlugin} from "../plugins/start-at-plugin";
import {CronPlugin} from "../plugins/cron-plugin";
import {SignalReceivedPlugin} from "../plugins/signal-received-plugin";
import {SendSignalPlugin} from "../plugins/send-signal-plugin";
import {StatePlugin} from "../plugins/state-plugin";
import {SendHttpRequestPlugin} from "../plugins/send-http-request-plugin";
import {DynamicOutcomesPlugin} from "../plugins/dynamic-outcomes-plugin";
import {ElsaStudio} from "../models";

export class PluginManager {
  plugins: Array<ElsaPlugin> = [];
  pluginTypes: Array<any> = [];
  elsaStudio: ElsaStudio;
  initialized: boolean;

  constructor() {
    this.pluginTypes = [
      DefaultDriversPlugin,
      ActivityIconProviderPlugin,
      IfPlugin,
      WhilePlugin,
      SwitchPlugin,
      HttpEndpointPlugin,
      SendHttpRequestPlugin,
      TimerPlugin,
      StartAtPlugin,
      CronPlugin,
      SignalReceivedPlugin,
      SendSignalPlugin,
      WriteLinePlugin,
      StatePlugin,
      SendEmailPlugin,
      DynamicOutcomesPlugin
    ];
  }

  initialize(elsaStudio: ElsaStudio) {
    if (this.initialized)
      return;

    this.elsaStudio = elsaStudio;

    for (const pluginType of this.pluginTypes) {
      this.createPlugin(pluginType);
    }
    this.initialized = true;
  }

  registerPlugins(pluginTypes: Array<any>) {
    for (const pluginType of pluginTypes) {
      this.registerPlugin(pluginType);
    }
  }

  registerPlugin(pluginType: any) {
    this.pluginTypes.push(pluginType);

    if (this.initialized)
      this.createPlugin(pluginType);
  }

  private createPlugin = (pluginType: any): ElsaPlugin => {
    return new pluginType(this.elsaStudio) as ElsaPlugin;
  }
}

export const pluginManager = new PluginManager();
