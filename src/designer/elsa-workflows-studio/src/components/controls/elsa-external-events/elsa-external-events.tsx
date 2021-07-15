import {Component, Host, h, Prop, Event, EventEmitter, State, Watch} from '@stencil/core';
import {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import {eventBus} from "../../../services/event-bus";
import {EventTypes} from "../../../models";
import {pluginManager, PluginManager} from "../../../services/plugin-manager";
import {ActivityIconProvider, activityIconProvider} from "../../../services/activity-icon-provider";

@Component({
  tag: 'elsa-external-events',
  shadow: false,
})
export class ElsaExternalEvents {

  @Event() httpClientConfigCreated: EventEmitter<AxiosRequestConfig>;
  @Event() httpClientCreated: EventEmitter<{ service: Service, axiosInstance: AxiosInstance }>;
  @Event() initializing: EventEmitter<{eventBus: any, pluginManager: PluginManager, activityIconProvider: ActivityIconProvider }>;

  connectedCallback() {
    eventBus.on(EventTypes.HttpClientConfigCreated, this.onHttpClientConfigCreated);
    eventBus.on(EventTypes.HttpClientCreated, this.onHttpClientCreated);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.HttpClientConfigCreated, this.onHttpClientConfigCreated);
    eventBus.detach(EventTypes.HttpClientCreated, this.onHttpClientCreated);
  }

  componentWillLoad() {
    this.initializing.emit({eventBus, pluginManager, activityIconProvider});
  }

  private onHttpClientConfigCreated = (config: AxiosRequestConfig) => {
    this.httpClientConfigCreated.emit(config);
  };

  private onHttpClientCreated = (service: Service, axiosInstance: AxiosInstance) => {
    this.httpClientCreated.emit({service, axiosInstance});
  };

  render() {
    return <Host/>;
  }
}
