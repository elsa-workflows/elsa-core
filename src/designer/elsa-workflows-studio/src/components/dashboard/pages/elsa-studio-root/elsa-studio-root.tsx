import {Component, Event, Method, EventEmitter, h, Host, Prop} from '@stencil/core';
import Tunnel, {DashboardState} from "../../../../data/dashboard";
import {ElsaStudio} from "../../../../models/services";
import {eventBus} from "../../../../services/event-bus";
import {pluginManager} from "../../../../services/plugin-manager";
import {activityIconProvider} from "../../../../services/activity-icon-provider";
import {createElsaClient, createHttpClient, ElsaClient} from "../../../../services/elsa-client";
import {AxiosInstance} from "axios";

@Component({
  tag: 'elsa-studio-root',
  shadow: false
})
export class ElsaStudioRoot {

  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path', reflect: true}) monacoLibPath: string;
  @Prop({attribute: 'culture', reflect: true}) culture: string;
  @Event() initializing: EventEmitter<ElsaStudio>;

  @Method()
  async addPlugins(pluginTypes: Array<any>) {
    pluginManager.registerPlugins(pluginTypes);
  }

  componentWillLoad() {
    const elsaClientFactory: () => ElsaClient = () => createElsaClient(this.serverUrl);
    const httpClientFactory: () => AxiosInstance = () => createHttpClient(this.serverUrl);

    const elsaStudio: ElsaStudio = {
      serverUrl: this.serverUrl,
      eventBus,
      pluginManager,
      activityIconProvider,
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
      </Tunnel.Provider>
    );
  }
}
