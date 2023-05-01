import 'reflect-metadata';
import {Component, Event, EventEmitter, h, Host, Prop} from '@stencil/core';
import {Container} from "typedi";
import {AuthContext, EventBus, PluginRegistry} from "../../services";
import {ShellInitializingContext} from "../../models/shell";
import {EventTypes} from "../../models";

@Component({
  tag: 'elsa-shell'
})
export class Shell {

  @Event() initializing: EventEmitter<ShellInitializingContext>;

  async componentWillLoad() {
    const pluginRegistry = Container.get(PluginRegistry);
    const context: ShellInitializingContext = {container: Container, pluginRegistry};
    this.initializing.emit(context);
  }

  render() {
    return <Host>
      <slot></slot>
    </Host>;
  }
}
