import {Component, Event, EventEmitter, h, Method, Prop} from '@stencil/core';
import Tunnel, {DashboardState} from "../../../../data/dashboard";
import {ElsaStudio} from "../../../../models";
import {eventBus, pluginManager, activityIconProvider, confirmDialogService, toastNotificationService, createElsaClient, createHttpClient, ElsaClient, propertyDisplayManager} from "../../../../services";
import {AxiosInstance} from "axios";
import {EventTypes} from "../../../../models";
import {ToastNotificationOptions} from "../../../shared/elsa-toast-notification/elsa-toast-notification";
import {getOrCreateProperty} from "../../../../utils/utils";

@Component({
  tag: 'elsa-studio-root',
  shadow: false
})
export class ElsaStudioRoot {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop({attribute: 'culture', reflect: true}) culture: string;
  @Prop({attribute: 'base-path', reflect: true}) basePath: string = '';
  @Event() initializing: EventEmitter<ElsaStudio>;

  confirmDialog: HTMLElsaConfirmDialogElement;
  toastNotificationElement: HTMLElsaToastNotificationElement;

  @Method()
  async addPlugins(pluginTypes: Array<any>) {
    pluginManager.registerPlugins(pluginTypes);
  }

  @Method()
  async addPlugin(pluginType: any) {
    pluginManager.registerPlugin(pluginType);
  }

  connectedCallback() {
    eventBus.on(EventTypes.ShowConfirmDialog, e => e.promise = this.confirmDialog.show(e.caption, e.message));
    eventBus.on(EventTypes.HideConfirmDialog, () => this.confirmDialog.hide());

    eventBus.on(EventTypes.ShowToastNotification, (e: ToastNotificationOptions) => this.toastNotificationElement.show(e));
    eventBus.on(EventTypes.HideToastNotification, () => this.toastNotificationElement.hide());
  }

  disconnectedCallback() {
    eventBus.off(EventTypes.ShowConfirmDialog);
    eventBus.off(EventTypes.HideConfirmDialog);
    eventBus.off(EventTypes.ShowToastNotification);
    eventBus.off(EventTypes.HideToastNotification);
  }

  componentWillLoad() {
    const elsaClientFactory: () => ElsaClient = () => createElsaClient(this.serverUrl);
    const httpClientFactory: () => AxiosInstance = () => createHttpClient(this.serverUrl);

    const elsaStudio: ElsaStudio = {
      serverUrl: this.serverUrl,
      basePath: this.basePath,
      eventBus,
      pluginManager,
      propertyDisplayManager,
      activityIconProvider,
      confirmDialogService,
      toastNotificationService,
      elsaClientFactory,
      httpClientFactory,
      getOrCreateProperty: getOrCreateProperty 
    };

    this.initializing.emit(elsaStudio);
    pluginManager.initialize(elsaStudio);
    propertyDisplayManager.initialize(elsaStudio);
  }

  render() {
    
    const culture = this.culture;

    const tunnelState: DashboardState = {
      serverUrl: this.serverUrl,
      basePath: this.basePath,
      culture,
      monacoLibPath: this.monacoLibPath
    };

    return (
      <Tunnel.Provider state={tunnelState}>
        <slot/>
        <elsa-confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture}/>
        <elsa-toast-notification ref={el => this.toastNotificationElement = el}/>
      </Tunnel.Provider>
    );
  }
}
