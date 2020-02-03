export class WorkflowPluginStore {
    constructor() {
        this.plugins = [];
        this.add = (plugin) => {
            this.plugins = [...this.plugins, plugin];
        };
        this.list = () => [...this.plugins];
    }
}
const pluginStore = new WorkflowPluginStore();
const win = window;
const elsa = win.elsa || {};
elsa.pluginStore = pluginStore;
win.elsa = elsa;
export default pluginStore;
