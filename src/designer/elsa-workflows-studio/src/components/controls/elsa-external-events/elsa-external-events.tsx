import {Component, Host, h, Prop, Event, EventEmitter, State, Watch} from '@stencil/core';
import {AxiosInstance, AxiosRequestConfig} from "axios";
import {Service} from 'axios-middleware';
import {eventBus} from "../../../services/event-bus";
import {EventTypes} from "../../../models";

@Component({
  tag: 'elsa-external-events',
  shadow: false,
})
export class ElsaExternalEvents {

  @Event() httpClientConfigCreated: EventEmitter<AxiosRequestConfig>;
  @Event() httpClientCreated: EventEmitter<{ service: Service, axiosInstance: AxiosInstance }>;

  connectedCallback() {
    eventBus.on(EventTypes.HttpClientConfigCreated, this.onHttpClientConfigCreated);
    eventBus.on(EventTypes.HttpClientCreated, this.onHttpClientCreated);
  }

  disconnectedCallback() {
    eventBus.detach(EventTypes.HttpClientConfigCreated, this.onHttpClientConfigCreated);
    eventBus.detach(EventTypes.HttpClientCreated, this.onHttpClientCreated);
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
