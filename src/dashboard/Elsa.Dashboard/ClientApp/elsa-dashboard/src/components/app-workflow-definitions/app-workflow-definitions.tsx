import { Component, h, State } from '@stencil/core';
import workflowDefinitionsApi from '../../services/workflow-definitions-api';
import { Workflow } from "@elsa-workflows/elsa-workflow-designer/dist/types/models";

@Component({
  tag: 'app-workflow-definitions',
  shadow: false
})
export class AppWorkflowDefinitions {

  @State()
  workflows: Array<Workflow> = [];

  componentWillLoad = async () => {
    this.workflows = await workflowDefinitionsApi.list();
  };

  onPublishClick = async (e: Event, workflow: Workflow) => {
    e.preventDefault();
    const publishedWorkflow = await workflowDefinitionsApi.publish(workflow.id);
    const index = this.workflows.findIndex(x => x.id == publishedWorkflow.id);
    const workflows = [...this.workflows];

    workflows[index] = publishedWorkflow;
    this.workflows = workflows;
  };

  onDeleteClick = async (e: Event, workflow: Workflow) => {
    e.preventDefault();

    if (!confirm('Are you sure you want to delete this workflow?'))
      return;

    await workflowDefinitionsApi.delete(workflow.id);
    this.workflows = this.workflows.filter(x => x.id !== workflow.id);
  };

  renderPublishAction = (workflow: Workflow) => {
    if (workflow.isPublished)
      return null;

    return (
      <li class="dropdown-item">
        <a href="#" onClick={ e => this.onPublishClick(e, workflow) }>Publish</a>
      </li>
    );
  };

  render() {
    const workflows = this.workflows;

    return (
      <div class="content-wrapper">
        <div class="content">
          <div class="row">
            <div class="col-12">
              <div class="float-right">
                <stencil-route-link url="/elsa-dashboard/workflow-definitions/new" class="btn btn-primary btn-default">New
                  Workflow
                </stencil-route-link>
              </div>
            </div>
          </div>
          <div class="row mt-2">
            <div class="col-12">

              <div class="card card-table-border-none" id="recent-orders">
                <div class="card-header justify-content-between">
                  <h2>Workflows</h2>
                </div>
                <div class="card-body pt-0 pb-5">
                  <table class="table card-table table-responsive table-responsive-large" style={ { "width": "100%" } }>
                    <thead>
                    <tr>
                      <th>ID</th>
                      <th>Name</th>
                      <th class="d-none d-md-table-cell">Halted</th>
                      <th class="d-none d-md-table-cell">Faulted</th>
                      <th class="d-none d-md-table-cell">Completed</th>
                      <th class="d-none d-md-table-cell">Status</th>
                      <th />
                    </tr>
                    </thead>
                    <tbody>
                    { workflows.map(workflow => {
                      const editUrl = `/elsa-dashboard/workflow-definitions/${ workflow.id }`;
                      return (
                        <tr>
                          <td>
                            <stencil-route-link class="text-dark" url={ editUrl }>{ workflow.id }</stencil-route-link>
                          </td>
                          <td>
                            <stencil-route-link class="text-dark" url={ editUrl }>{ workflow.name }</stencil-route-link>
                          </td>
                          <td><a href="#" class="badge badge-info">0</a></td>
                          <td><a href="#" class="badge badge-danger">0</a></td>
                          <td><a href="#" class="badge badge-success">0</a></td>
                          <td><span>{ workflow.isPublished ? 'Published' : 'Draft' }</span></td>
                          <td class="text-right">
                            <div class="dropdown show d-inline-block widget-dropdown">
                              <a class="dropdown-toggle icon-burger-mini" href="javascript:void(0)" role="button" id="dropdown-recent-order1" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" data-display="static" />
                              <ul class="dropdown-menu dropdown-menu-right" aria-labelledby="dropdown-recent-order1">
                                <li class="dropdown-item">
                                  <stencil-route-link url={ editUrl }>Edit</stencil-route-link>
                                </li>
                                { this.renderPublishAction(workflow) }
                                <li class="dropdown-item">
                                  <a href="#" onClick={ e => this.onDeleteClick(e, workflow) }>Delete</a>
                                </li>
                              </ul>
                            </div>
                          </td>
                        </tr>
                      );
                    }) }
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
