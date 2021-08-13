import {Component, h, Prop, State, Watch} from '@stencil/core';
import {WorkflowDefinition} from "../../../../models";
import {createElsaClient} from "../../../../services";
import Tunnel from "../../../../data/dashboard";
import {ElsaWorkflowPropertiesPanel} from "../../workflow-definition-editor/elsa-workflow-properties-panel/elsa-workflow-properties-panel";

@Component({
  tag: 'elsa-workflow-blueprint-side-panel',
  shadow: false,
})
export class ElsaWorkflowBlueprintSidePanel {

  @Prop() workflowId: string;
  @Prop() serverUrl: string;
  @State() workflowDefinition: WorkflowDefinition;

  @Watch('workflowId')
  async workflowIdChangedHandler(newWorkflowId: string) {
    await this.loadWorkflowDefinition(newWorkflowId);
  }

  render() {
    return (
      <elsa-workflow-properties-panel
        workflowDefinition={this.workflowDefinition}
      />
    );
  }

  async componentWillLoad() {
    await this.loadWorkflowDefinition();
  }

  createClient() {
    return createElsaClient(this.serverUrl);
  }

  async loadWorkflowDefinition(workflowId = this.workflowId) {
    const elsaClient = this.createClient();

    this.workflowDefinition = await elsaClient.workflowDefinitionsApi.getByDefinitionAndVersion(workflowId, {isLatest: true});
  }
}

Tunnel.injectProps(ElsaWorkflowPropertiesPanel, ['serverUrl']);
