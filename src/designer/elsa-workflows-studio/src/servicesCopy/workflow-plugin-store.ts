import { WorkflowPlugin } from "../modelsCopy";
import { Elsa } from "./elsa";

export class WorkflowPluginStore {

  private plugins: Array<WorkflowPlugin> = [];

  add = (plugin: WorkflowPlugin) => {
    this.plugins = [...this.plugins, plugin];
  };

  list = (): Array<WorkflowPlugin> => [...this.plugins];
}

const pluginStore = new WorkflowPluginStore();
const win = window as any;
const elsa = win.elsa as Elsa || {} as Elsa;

elsa.pluginStore = pluginStore;
win.elsa = elsa;

export default pluginStore;
