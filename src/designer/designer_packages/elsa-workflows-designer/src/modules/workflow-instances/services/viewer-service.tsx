import {h} from '@stencil/core';
import {Service} from "typedi";
import {WorkflowInstance} from "../../../models";
import {WorkflowInstanceViewerInstance} from "./viewer-instance";
import {WorkflowDefinition} from "../../workflow-definitions/models/entities";

@Service()
export class WorkflowInstanceViewerService {
  public show = (workflowDefinition: WorkflowDefinition, workflowInstance: WorkflowInstance): WorkflowInstanceViewerInstance => {
    return new WorkflowInstanceViewerInstance(workflowDefinition, workflowInstance);
  };
}

