import {ElsaPlugin} from "./elsa-plugin";
import {IfPlugin} from "../plugins/if-plugin";
import {HttpEndpointPlugin} from "../plugins/http-endpoint-plugin";
import {TimerPlugin} from "../plugins/timer-plugin";
import {WriteLinePlugin} from "../plugins/write-line-plugin";
import {SendEmailPlugin} from "../plugins/send-email-plugin";

export class PluginManager {

  plugins: ElsaPlugin = [];

  constructor() {
    this.plugins = [
      new IfPlugin(),
      new HttpEndpointPlugin(),
      new TimerPlugin(),
      new WriteLinePlugin(),
      new SendEmailPlugin()
    ];
  }

  initialize() {
  }
}

export const pluginManager = new PluginManager();
