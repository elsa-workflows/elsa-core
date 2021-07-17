import {PluginManager, ActivityIconProvider, ConfirmDialogService} from "../services";
import {ElsaClient} from "../services/elsa-client";
import EventBus from "js-event-bus";
import {AxiosInstance} from "axios";

export interface ElsaStudio {
  serverUrl: string;
  pluginManager: PluginManager;
  elsaClientFactory: () => ElsaClient;
  httpClientFactory: () => AxiosInstance;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
  confirmDialogService: ConfirmDialogService;
}