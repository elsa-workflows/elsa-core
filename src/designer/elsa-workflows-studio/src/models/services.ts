import {PluginManager} from "../services/plugin-manager";
import {ElsaClient} from "../services/elsa-client";
import EventBus from "js-event-bus";
import {ActivityIconProvider} from "../services/activity-icon-provider";
import {AxiosInstance} from "axios";

export interface ElsaStudio {
  serverUrl: string;
  pluginManager: PluginManager;
  elsaClientFactory: () => ElsaClient;
  httpClientFactory: () => AxiosInstance;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
}