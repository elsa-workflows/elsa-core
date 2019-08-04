import { Component, h, Prop, State } from '@stencil/core';
import '@elsa-workflows/elsa-workflow-designer';
import workflowDefinitionsApi from '../../services/workflow-definitions-api';
import { MatchResults } from "@stencil/router";
import jsonPatch from 'fast-json-patch';
import {
  Workflow,
  WorkflowFormatDescriptor,
  WorkflowFormatDescriptorDictionary
} from "@elsa-workflows/elsa-workflow-designer/dist/types/models";

@Component({
  tag: 'app-workflow-editor',
  styleUrl: 'app-workflow-editor.scss',
  shadow: false
})
export class AppWorkflowEditor {

  designer: HTMLWfDesignerHostElement;
  workflow: Workflow;

  @Prop() match: MatchResults;

  @Prop()
  workflowFormats: WorkflowFormatDescriptorDictionary = {
    json: {
      format: 'json',
      fileExtension: '.json',
      mimeType: 'application/json',
      displayName: 'JSON'
    },
    yaml: {
      format: 'yaml',
      fileExtension: '.yaml',
      mimeType: 'application/x-yaml',
      displayName: 'YAML'
    },
    xml: {
      format: 'xml',
      fileExtension: '.xml',
      mimeType: 'application/xml',
      displayName: 'XML'
    }
  };

  addActivity = async () => {
    await this.designer.showActivityPicker();
  };

  importWorkflow = async () => {
    await this.designer.import();
  };

  createNewWorkflow = async () => {
    if (confirm('Are you sure you want to discard current changes?'))
      await this.designer.newWorkflow();
  };

  @State()
  workflowName: string;

  @State()
  workflowDescription: string;

  async componentWillLoad() {
    const id = this.match.params.id;
    this.workflow = await workflowDefinitionsApi.getById(id);
    this.workflowName = this.workflow.name;
  }

  export = async (descriptor: WorkflowFormatDescriptor) => {
    await this.designer.export(descriptor);
  };

  onExportClick = async (e: Event, descriptor: WorkflowFormatDescriptor) => {
    e.preventDefault();
    await this.export(descriptor);
  };

  onNameKeyUp = (e: Event) => {
    const input = e.target as HTMLInputElement;
    this.workflowName = input.value;
  };

  onDescriptionKeyUp = (e: Event) => {
    const input = e.target as HTMLTextAreaElement;
    this.workflowDescription = input.value;
  };

  onSaveDraftClick = async (e: Event) => {
    e.preventDefault();

    const workflow = await this.designer.readWorkflow();

    let updatedWorkflow: Workflow = {
      ...workflow,
      name: this.workflowName,
      description: this.workflowDescription
    };

    const patch = jsonPatch.compare(this.workflow, updatedWorkflow);
    await workflowDefinitionsApi.patch(updatedWorkflow.id, patch);
    this.workflow = updatedWorkflow;
  };

  render() {
    const workflow = this.workflow || {
      activities: [],
      connections: []
    };

    const descriptors = this.workflowFormats;

    return (
      <div class="content-wrapper">
        <div class="content">
          <div class="breadcrumb-wrapper">
            <h1>{ this.workflowName }</h1>
          </div>
          <div class="row mt-2">
            <div class="col-9">
              <div class="card card-default">
                <div class="card-header card-header-border-bottom">
                  <ul class="nav">
                    <li class="nav-item">
                      <button class="btn btn-primary" onClick={ () => this.addActivity() }>Add Activity</button>
                    </li>
                    <li class="nav-item">
                      <div class="dropdown">
                        <button class="btn btn-secondary dropdown-toggle" type="button" id="exportButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                          Export
                        </button>
                        <div class="dropdown-menu" aria-labelledby="exportButton">
                          { Object.keys(descriptors).map(key => {
                            const descriptor = descriptors[key];
                            return (
                              <a class="dropdown-item" href="#" onClick={ e => this.onExportClick(e, descriptor) }>{ descriptor.displayName }</a>);
                          }) }
                        </div>
                      </div>
                    </li>
                    <li class="nav-item">
                      <button class="btn btn-secondary" onClick={ () => this.importWorkflow() }>Import</button>
                    </li>
                    <li class="nav-item">
                      <button class="btn btn-secondary" onClick={ () => this.createNewWorkflow() }>New Workflow</button>
                    </li>
                    <li class="nav-item">
                      <div class="dropdown d-inline-block mb-1">
                        <button class="btn btn-success dropdown-toggle" type="button" id="saveButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" data-display="static">
                          Save
                        </button>
                        <div class="dropdown-menu" aria-labelledby="saveButton">
                          <a class="dropdown-item" href="#" onClick={ this.onSaveDraftClick }>Draft</a>
                          <a class="dropdown-item" href="#">Publish</a>
                        </div>
                      </div>
                    </li>
                  </ul>
                </div>
                <div class="m-2">
                  <wf-designer-host workflow={ workflow } ref={ el => this.designer = el } />
                </div>
              </div>
            </div>
            <div class="col-3">
              <div class="card card-default">
                <div class="card-header card-header-border-bottom">
                  <ul class="nav nav-pills mb-3" id="pills-tab" role="tablist">
                    <li class="nav-item">
                      <a class="nav-link active" id="pills-properties-tab" data-toggle="pill" href="#pills-properties" role="tab" aria-controls="pills-properties" aria-selected="true">Properties</a>
                    </li>
                    <li class="nav-item">
                      <a class="nav-link" id="pills-history-tab" data-toggle="pill" href="#pills-history" role="tab" aria-controls="pills-history" aria-selected="false">History</a>
                    </li>
                  </ul>
                </div>
                <div class="card-body">
                  <div class="tab-content" id="pills-tabContent">
                    <div class="tab-pane fade active show" id="pills-properties" role="tabpanel" aria-labelledby="pills-properties-tab">
                      <form>
                        <div class="form-group">
                          <label>Version</label>&nbsp;<span>{ workflow.version }</span>
                        </div>
                        <div class="form-group">
                          <label>Name</label>
                          <input type="text" class="form-control" value={ this.workflowName } onKeyUp={ this.onNameKeyUp } />
                        </div>
                        <div class="form-group">
                          <label>Description</label>
                          <textarea class="form-control" rows={ 9 } onKeyUp={ this.onDescriptionKeyUp }>{ this.workflowDescription }</textarea>
                        </div>
                      </form>
                    </div>
                    <div class="tab-pane fade" id="pills-history" role="tabpanel" aria-labelledby="pills-history-tab">
                      List of revisions...
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
