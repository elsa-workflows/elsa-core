﻿import {ElsaPlugin} from "./elsa-plugin";
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

export class PluginManager {

  plugins: ElsaPlugin = [];

  constructor() {
    this.plugins = [
      new DefaultDriversPlugin(),
      new ActivityIconProviderPlugin(),
      new IfPlugin(),
      new ForkPlugin(),
      new SwitchPlugin(),
      new HttpEndpointPlugin(),
      new TimerPlugin(),
      new WriteLinePlugin(),
      new RunJavascriptPlugin(),
      new SendEmailPlugin()
    ];
  }

  initialize() {
  }
}

export const pluginManager = new PluginManager();
