import {Component, h, Prop, State, Watch} from '@stencil/core';
import {WorkflowBlueprint} from "../../../../models";
import {createElsaClient} from "../../../../services";
import Tunnel from "../../../../data/dashboard";

@Component({
  tag: 'elsa-workflow-blueprint-side-panel',
  shadow: false,
})
export class ElsaWorkflowBlueprintSidePanel {

  @Prop() workflowId: string;
  @Prop() serverUrl: string;
  @State() workflowBlueprint: WorkflowBlueprint;

  @Watch('workflowId')
  async workflowIdChangedHandler(newWorkflowId: string) {
    await this.loadWorkflowBlueprint(newWorkflowId);
  }

  render() {
    return (
      <elsa-workflow-blueprint-properties-panel
        workflowBlueprint={this.workflowBlueprint}
      />
    );
  }

  async componentWillLoad() {
    await this.loadWorkflowBlueprint();
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  async loadWorkflowBlueprint(workflowId = this.workflowId) {
    const elsaClient = await this.createClient();

    this.workflowBlueprint = await elsaClient.workflowRegistryApi.get(workflowId, {isLatest: true});
  }
}

Tunnel.injectProps(ElsaWorkflowBlueprintSidePanel, ['serverUrl']);
