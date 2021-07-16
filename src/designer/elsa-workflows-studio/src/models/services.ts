import {PluginManager} from "../services/plugin-manager";
import {ElsaClient} from "../services/elsa-client";
import EventBus from "js-event-bus";
import {ActivityIconProvider} from "../services/activity-icon-provider";

export interface ElsaStudio {
  pluginManager: PluginManager;
  elsaClientFactory: () => ElsaClient;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
}