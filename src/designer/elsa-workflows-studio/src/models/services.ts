import {PluginManager, ActivityIconProvider, ConfirmDialogService} from "../services";
import {ElsaClient} from "../services/elsa-client";
import EventBus from "js-event-bus";
import {AxiosInstance} from "axios";
import {ToastNotificationService} from "../services/toast-notification-service";

export interface ElsaStudio {
  serverUrl: string;
  basePath: string;
  pluginManager: PluginManager;
  elsaClientFactory: () => ElsaClient;
  httpClientFactory: () => AxiosInstance;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
  confirmDialogService: ConfirmDialogService;
  toastNotificationService: ToastNotificationService;
}