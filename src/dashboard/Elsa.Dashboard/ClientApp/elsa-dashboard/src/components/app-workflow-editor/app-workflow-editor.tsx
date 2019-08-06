import { Component, h, Prop, State, Watch } from '@stencil/core';
import '@elsa-workflows/elsa-workflow-designer';
import workflowDefinitionsApi from '../../services/workflow-definitions-api';
import { MatchResults } from "@stencil/router";
import jsonPatch from 'fast-json-patch';
import * as Ladda from 'ladda';
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
  saveButton: HTMLButtonElement;
  publishButton: HTMLButtonElement;

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
  workflow: Workflow;

  @State()
  isDirty: boolean;

  oldWorkflow: Workflow;
  workflowId: string;

  async componentWillLoad() {
  }

  async componentDidLoad() {
    const id = this.match.params.id;

    if (id === 'new') {
      this.workflowId = null;
      this.workflow = {
        name: 'New Workflow',
        activities: [],
        connections: []
      };
    } else {
      this.workflowId = id;
      this.workflow = await workflowDefinitionsApi.getById(id);
    }

    this.oldWorkflow = this.workflow;

  }

  export = async (descriptor: WorkflowFormatDescriptor) => {
    await this.designer.export(descriptor);
  };

  save = async () => {
    const workflow = await this.designer.getWorkflow();

    if (!!this.workflowId) {
      const patch = jsonPatch.compare(this.oldWorkflow, workflow);

      if (patch.length > 0) {
        const updatedWorkflow = await workflowDefinitionsApi.patch(this.workflowId, patch);
        this.workflow = this.oldWorkflow = updatedWorkflow;
      }
    } else {
      const createdWorkflow = await workflowDefinitionsApi.post(workflow);
      this.workflow = this.oldWorkflow = createdWorkflow;
      this.workflowId = createdWorkflow.id;
    }
  };

  publish = async (id: string): Promise<Workflow> => {
    return workflowDefinitionsApi.publish(id)
  };

  onWorkflowChanged = (e: CustomEvent<Workflow>) => {
    const workflow = e.detail;
    const patch = jsonPatch.compare(this.oldWorkflow, workflow);
    this.isDirty = patch.length > 0;

    console.debug(`dirty: ${ this.isDirty }`);
  };

  onExportClick = async (e: Event, descriptor: WorkflowFormatDescriptor) => {
    e.preventDefault();
    await this.export(descriptor);
  };

  onNameKeyUp = (e: Event) => {
    const input = e.target as HTMLInputElement;
    this.workflow = { ...this.workflow, name: input.value };
  };

  onDescriptionKeyUp = (e: Event) => {
    const input = e.target as HTMLTextAreaElement;
    this.workflow = { ...this.workflow, description: input.value };
  };

  onSaveDraftClick = async (e: Event) => {
    e.preventDefault();

    const laddaButton = Ladda.create(this.saveButton).start();

    try {
      await this.save();
    } finally {
      laddaButton.stop();
    }
  };

  onPublishClick = async (e: Event) => {
    e.preventDefault();

    const laddaButton = Ladda.create(this.publishButton).start();

    try {
      await this.save();
      await this.publish(this.workflowId);
    } finally {
      laddaButton.stop();
    }
  };

  render() {
    const workflow = this.workflow || {
      activities: [],
      connections: [],
      isPublished: false,
      name: null,
      description: null,
      version: 0
    };

    const descriptors = this.workflowFormats;

    return (
      <div class="content-wrapper">
        <div class="content">
          <div class="breadcrumb-wrapper">
            <h1>{ workflow.name }</h1>
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
                      <button class="btn btn-success ladda-button"
                              type="button" id="saveButton"
                              data-style="expand-right"
                              disabled={ !this.isDirty }
                              onClick={ this.onSaveDraftClick }
                              ref={ el => this.saveButton = el }>
                        <span class="ladda-label">Save Draft</span>
                      </button>
                    </li>
                    <li class="nav-item">
                      <button class="btn btn-success ladda-button"
                              type="button" id="publishButton"
                              data-style="expand-right"
                              disabled={ !this.isDirty && workflow.isPublished }
                              onClick={ this.onPublishClick }
                              ref={ el => this.publishButton = el }>
                        <span class="ladda-label">Publish</span>
                      </button>
                    </li>
                  </ul>
                </div>
                <div class="m-2">
                  <wf-designer-host workflow={ workflow } ref={ el => this.designer = el } onWorkflowChanged={ this.onWorkflowChanged } canvasHeight={ '300vh' } />
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
                          <input type="text" class="form-control" value={ workflow.name } onKeyUp={ this.onNameKeyUp } />
                        </div>
                        <div class="form-group">
                          <label>Description</label>
                          <textarea class="form-control" rows={ 9 } onKeyUp={ this.onDescriptionKeyUp }>{ workflow.description }</textarea>
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
