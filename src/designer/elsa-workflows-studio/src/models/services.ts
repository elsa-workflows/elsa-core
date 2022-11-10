import {PluginManager, ActivityIconProvider, ConfirmDialogService, PropertyDisplayManager} from "../services";
import {ElsaClient, ToastNotificationService} from "../services";
import EventBus from "../services/custom-event-bus";
import {AxiosInstance} from "axios";
import {ActivityDefinitionProperty} from "./domain";
import {ActivityModel} from "./view";

export interface ElsaStudio {
  serverUrl: string;
  basePath: string;
  features: any;
  serverFeatures: Array<string>;
  serverVersion: string;
  pluginManager: PluginManager;
  propertyDisplayManager: PropertyDisplayManager;
  elsaClientFactory: () => Promise<ElsaClient>;
  httpClientFactory: () => Promise<AxiosInstance>;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
  confirmDialogService: ConfirmDialogService;
  toastNotificationService: ToastNotificationService;
  getOrCreateProperty: (activity: ActivityModel, name: string, defaultExpression?: () => string, defaultSyntax?: () => string) => ActivityDefinitionProperty;
  htmlToElement: (html: string) => Element;
}
