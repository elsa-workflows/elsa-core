import {h} from '@stencil/core';
import {Service} from "typedi";
import {WorkflowDefinition} from "../models/entities";
import {WorkflowDefinitionEditorInstance} from "./editor-instance";

@Service()
export class WorkflowDefinitionEditorService {
  public show = (workflowDefinition: WorkflowDefinition): WorkflowDefinitionEditorInstance => {

    return new WorkflowDefinitionEditorInstance(workflowDefinition);
  };
}

