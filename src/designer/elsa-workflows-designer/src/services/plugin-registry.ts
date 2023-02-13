import 'reflect-metadata';
import {Container, Service} from "typedi";
import {SwitchPlugin} from "../modules/switch/switch-plugin";
import {Plugin} from "../models";
import {SequencePlugin} from "../modules/sequence/sequence-plugin";
import {FlowSwitchPlugin} from "../modules/flow-switch/flow-switch-plugin";
import {WorkflowDefinitionsPlugin} from "../modules/workflow-definitions/plugin";
import {WorkflowInstancesPlugin} from "../modules/workflow-instances/plugin";
import {LoginPlugin} from "../modules/login/plugin";
import {HomePagePlugin} from "../modules/home/plugin";
import {FlowchartPlugin} from "../modules/flowchart/plugin";

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
    this.add(Container.get(SequencePlugin));
    this.add(Container.get(SwitchPlugin));
    this.add(Container.get(FlowSwitchPlugin));
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
