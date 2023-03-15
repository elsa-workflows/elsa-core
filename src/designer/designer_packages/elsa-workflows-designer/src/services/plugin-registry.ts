import 'reflect-metadata';
import {Container, Service} from "typedi";
import {Plugin} from "../models";
import {SequencePlugin} from "../modules/sequence/sequence-plugin";
import {WorkflowDefinitionsPlugin} from "../modules/workflow-definitions/plugins/workflow-definitions-plugin";
import {CompositeActivityVersionPlugin} from "../modules/workflow-definitions/plugins/composite-version-plugin";
import {WorkflowInstancesPlugin} from "../modules/workflow-instances/plugin";
import {LoginPlugin} from "../modules/login/plugin";
import {HomePagePlugin} from "../modules/home/plugin";
import {FlowchartPlugin} from "../modules/flowchart/plugin";
import { SwitchPlugin } from '../modules/switch/sequence/switch-plugin';
import {FlowSwitchPlugin} from "../modules/switch/flow/flow-switch-plugin";
import {FlowHttpRequestPlugin} from "../modules/http-request/flow/flow-http-request-plugin";
import {HttpRequestPlugin} from "../modules/http-request/sequence/http-request-plugin";

// A registry of plugins.
@Service()
export class PluginRegistry {
  private readonly plugins: Array<Plugin> = [];

  constructor() {
    this.add(Container.get(FlowchartPlugin))
    this.add(Container.get(LoginPlugin));
    this.add(Container.get(HomePagePlugin));
    this.add(Container.get(WorkflowDefinitionsPlugin));
    this.add(Container.get(WorkflowInstancesPlugin));
    this.add(Container.get(CompositeActivityVersionPlugin));
    this.add(Container.get(SequencePlugin));
    this.add(Container.get(SwitchPlugin));
    this.add(Container.get(FlowSwitchPlugin));
    this.add(Container.get(FlowHttpRequestPlugin));
    this.add(Container.get(HttpRequestPlugin));
  }

  add(plugin: Plugin) {
    this.plugins.push(plugin);
  }

  async initialize(): Promise<void> {
    for (const plugin of this.plugins) {
      await plugin.initialize();
    }
  }
}
