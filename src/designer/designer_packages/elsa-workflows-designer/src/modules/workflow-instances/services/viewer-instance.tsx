import {h} from "@stencil/core";
import {WorkflowInstance} from "../../../models";
import studioComponentStore from "../../../data/studio-component-store";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";

export class WorkflowInstanceViewerInstance {
  private workflowInstanceViewerElement: HTMLElsaWorkflowInstanceViewerElement;

  constructor(
    private workflowDefinition: WorkflowDefinition,
    private workflowInstance: WorkflowInstance) {

    studioComponentStore.activeComponentFactory = () =>
      <elsa-workflow-instance-viewer
        workflowDefinition={workflowDefinition}
        workflowInstance={workflowInstance}
        ref={el => this.workflowInstanceViewerElement = el}/>;
  }
}
