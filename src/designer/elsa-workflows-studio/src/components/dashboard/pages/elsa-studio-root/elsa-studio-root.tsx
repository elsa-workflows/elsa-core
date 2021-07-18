import {Component, Event, EventEmitter, h, Method, Prop} from '@stencil/core';
import Tunnel, {DashboardState} from "../../../../data/dashboard";
import {ElsaStudio} from "../../../../models";
import {eventBus, pluginManager, activityIconProvider, confirmDialogService, createElsaClient, createHttpClient, ElsaClient} from "../../../../services";
import {AxiosInstance} from "axios";
import {EventTypes} from "../../../../models";
import {ToastNotificationOptions} from "../../../shared/elsa-toast-notification/elsa-toast-notification";
import {toastNotificationService} from "../../../../services/toast-notification-service";

@Component({
  tag: 'elsa-studio-root',
  shadow: false
})
export class ElsaStudioRoot {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop({attribute: 'culture', reflect: true}) culture: string;
  @Event() initializing: EventEmitter<ElsaStudio>;

  confirmDialog: HTMLElsaConfirmDialogElement;
  toastNotificationElement: HTMLElsaToastNotificationElement;

  @Method()
  async addPlugins(pluginTypes: Array<any>) {
    pluginManager.registerPlugins(pluginTypes);
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
      eventBus,
      pluginManager,
      activityIconProvider,
      confirmDialogService,
      toastNotificationService,
      elsaClientFactory,
      httpClientFactory
    };

    this.initializing.emit(elsaStudio);
    pluginManager.initialize(elsaStudio);
  }

  render() {

    const serverUrl = this.serverUrl;
    const culture = this.culture;
    const monacoLibPath = this.monacoLibPath;

    const tunnelState: DashboardState = {
      serverUrl,
      culture,
      monacoLibPath
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
