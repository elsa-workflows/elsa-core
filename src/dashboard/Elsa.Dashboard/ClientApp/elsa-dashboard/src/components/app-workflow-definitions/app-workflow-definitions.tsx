import { Component, h } from '@stencil/core';
import workflowDefinitionsApi from '../../services/workflow-definitions-api';
import { Workflow } from "@elsa-workflows/elsa-workflow-designer/dist/types/models";

@Component({
  tag: 'app-workflow-definitions',
  shadow: false
})
export class AppWorkflowDefinitions {

  workflows: Array<Workflow> = [];

  componentWillLoad = async () => {
    this.workflows = await workflowDefinitionsApi.list();
  };

  render() {
    const workflows = this.workflows;

    return (
      <div class="content-wrapper">
        <div class="content">
          <div class="row">
            <div class="col-12">
              <div class="float-right">
                <a href="#" class="btn btn-primary btn-default">New Workflow</a>
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
                    { workflows.map(definition => {
                      const editUrl = `/elsa-dashboard/workflow-definitions/${definition.id}`;
                      return (
                        <tr>
                          <td>
                            <stencil-route-link class="text-dark" url={ editUrl }>{ definition.id }</stencil-route-link>
                          </td>
                          <td>
                            <stencil-route-link class="text-dark" url={ editUrl }>{ definition.name }</stencil-route-link>
                          </td>
                          <td><a href="#" class="badge badge-info">0</a></td>
                          <td><a href="#" class="badge badge-danger">0</a></td>
                          <td><a href="#" class="badge badge-success">0</a></td>
                          <td><span>Published</span></td>
                          <td class="text-right">
                            <div class="dropdown show d-inline-block widget-dropdown">
                              <a class="dropdown-toggle icon-burger-mini" href="javascript:void(0)" role="button" id="dropdown-recent-order1" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" data-display="static" />
                              <ul class="dropdown-menu dropdown-menu-right" aria-labelledby="dropdown-recent-order1">
                                <li class="dropdown-item">
                                  <stencil-route-link url={ editUrl }>{ definition.name }</stencil-route-link>
                                </li>
                                <li class="dropdown-item">
                                  <a href="#">Publish</a>
                                </li>
                                <li class="dropdown-item">
                                  <a href="#">Remove</a>
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
