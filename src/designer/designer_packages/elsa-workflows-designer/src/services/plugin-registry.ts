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
import {SwitchPlugin} from '../modules/switch/sequence/switch-plugin';
import {FlowSwitchPlugin} from "../modules/switch/flow/flow-switch-plugin";
import {FlowHttpRequestPlugin} from "../modules/http-request/flow/flow-http-request-plugin";
import {HttpRequestPlugin} from "../modules/http-request/sequence/http-request-plugin";
import {WorkflowContextsPlugin} from "../modules/workflow-contexts/plugin";
import {DescriptorsPlugin} from "../modules/descriptors/plugin";

// A registry of plugins.
@Service()
export class PluginRegistry {
  private readonly pluginTypes: Map<string, any> = new Map<string, any>();

  constructor() {
    const add = this.add;

    add('descriptors', DescriptorsPlugin);
    add('flowchart', FlowchartPlugin);
    add('login', LoginPlugin);
    add('home', HomePagePlugin);
    add('workflow-definitions', WorkflowDefinitionsPlugin);
    add('workflow-instances', WorkflowInstancesPlugin);
    add('composite-activity-version', CompositeActivityVersionPlugin);
    add('sequence', SequencePlugin);
    add('switch', SwitchPlugin);
    add('flow-switch', FlowSwitchPlugin);
    add('flow-http-request', FlowHttpRequestPlugin);
    add('http-request', HttpRequestPlugin);
    add('workflow-contexts', WorkflowContextsPlugin);
  }

  add = (name: string, plugin: any) => {
    this.pluginTypes.set(name, plugin);
  };

  replace = (name: string, plugin: any) => {
    this.pluginTypes.set(name, plugin);
  };

  remove = (name: string) => {
    this.pluginTypes.delete(name);
  };

  initialize = async (): Promise<void> => {
    for (const pluginType of this.pluginTypes.values()) {

      if (!Container.has(pluginType))
        Container.set(pluginType, new pluginType());

      const plugin = Container.get(pluginType) as Plugin;

      if (!!plugin.initialize)
        await plugin.initialize();
    }
  };
}
