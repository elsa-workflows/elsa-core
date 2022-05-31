import 'reflect-metadata';
import {Container, Service} from "typedi";
import {SwitchActivity} from "./models";
import {EventBus} from "../../services";
import {SwitchPlugin} from "./switch-plugin";
import {ActivityPropertyChangedEventArgs, WorkflowEditorEventTypes} from "../../components/designer/workflow-definition-editor/models";

@Service()
export class SwitchPortUpdater {

  constructor() {
    const eventBus = Container.get(EventBus);

    eventBus.on(WorkflowEditorEventTypes.Activity.PropertyChanged, this.onActivityPropertyChanged);
  }

  private onActivityPropertyChanged = async (e: ActivityPropertyChangedEventArgs) => {

    const activity = e.activity;
    const propertyName = e.propertyName;

    if (activity.typeName !== SwitchPlugin.ActivityTypeName || propertyName !== 'cases')
      return;

    const switchActivity = activity as SwitchActivity;
    const cases = switchActivity.cases;

    const workflowEditor = e.workflowEditor;
    const canvas = await workflowEditor.getCanvas();
    const flowChart = (await canvas.getRootComponent()) as HTMLElsaFlowchartElement;
    const graph = await flowChart.getGraph();

    const node = graph.getNodes().find(x => x.data.id === activity.id);

    if (!node)
      return;

    // Remove ports.
    const ports = [...node.getPortsByGroup('out')];
    for (const port of ports) {
      if (cases.find(x => x.label == port.id) == null)
        node.removePort(port);
    }

    // Add new ports.
    for (const c of cases) {
      if (!node.hasPort(c.label))
        node.addPort({
          id: c.label, group: 'out', attrs: {
            text: {
              text: c.label
            }
          }
        })
    }
  }
}
