import {PluginManager, ActivityIconProvider, ConfirmDialogService, PropertyDisplayManager, FeatureProvider} from "../services";
import {ElsaClient, ToastNotificationService} from "../services";
import EventBus from "js-event-bus";
import {AxiosInstance} from "axios";
import {ActivityDefinitionProperty} from "./domain";
import {ActivityModel} from "./view";

export interface ElsaStudio {
  serverUrl: string;
  basePath: string;
  featuresString: string;
  featureProvider: FeatureProvider;
  pluginManager: PluginManager;
  propertyDisplayManager: PropertyDisplayManager;
  elsaClientFactory: () => ElsaClient;
  httpClientFactory: () => AxiosInstance;
  eventBus: EventBus;
  activityIconProvider: ActivityIconProvider;
  confirmDialogService: ConfirmDialogService;
  toastNotificationService: ToastNotificationService;
  getOrCreateProperty: (activity: ActivityModel, name: string, defaultExpression?: () => string, defaultSyntax?: () => string) => ActivityDefinitionProperty;
  htmlToElement: (html: string) => Element;
}