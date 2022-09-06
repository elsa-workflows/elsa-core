import {Component, Event, EventEmitter, h, Prop, Watch} from '@stencil/core';
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {WorkflowDefinition} from "../models/entities";
import {WorkflowDefinitionsApi} from "../../workflow-definitions/services/api";
import {DeleteIcon, RevertIcon, PublishedIcon} from "../../../components/icons/tooling";
import moment from "moment";

@Component({
  tag: 'elsa-workflow-definition-version-history',
})
export class WorkflowDefinitionVersionHistory {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionApi: WorkflowDefinitionsApi;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionApi = Container.get(WorkflowDefinitionsApi);
  }

  @Prop() selectedVersion: WorkflowDefinition;
  @Prop() workflowVersions: Array<WorkflowDefinition>;
  @Prop() serverUrl: string;
  @Event() versionSelected: EventEmitter<WorkflowDefinition>;
  @Event() deleteVersionClicked: EventEmitter<WorkflowDefinition>;
  @Event() revertVersionClicked: EventEmitter<WorkflowDefinition>;

  @Watch('workflowDefinition')
  async onWorkflowDefinitionChanged(value: WorkflowDefinition) {
    
  }

  async componentWillLoad() {
  
  }

  onViewVersionClick = (e: Event, version: WorkflowDefinition) => {
    e.preventDefault();
    this.versionSelected.emit(version);
  };

  onDeleteVersionClick = async (e: Event, version: WorkflowDefinition) => {
    e.preventDefault();
    this.deleteVersionClicked.emit(version);
  };

  onRevertVersionClick = (e: Event, version: WorkflowDefinition) => {
    e.preventDefault();
    this.revertVersionClicked.emit(version);
  };

  render() {
    return (
      <div>
        <table>
          <thead>
            <tr>
              <th/>
              <th>Version</th>
              <th>Created</th>
              <th/>
              <th/>
            </tr>
          </thead>
          <tbody>
          {this.workflowVersions.map(v => {
              return (
                <tr>
                  <td>{v.isPublished ? <PublishedIcon/> : ""}</td>
                  <td>{v.version}</td>
                  <td>{moment(v.createdAt).format('DD-MM-YYYY HH:mm:ss')}</td>
                  <td>
                    <button onClick={e => this.onViewVersionClick(e, v)}
                            type="button"
                            disabled={this.selectedVersion.version == v.version}
                            class={this.selectedVersion.version == v.version ? "btn btn-secondary" : "btn btn-primary"}>
                      View
                    </button>
                  </td>
                  <td>
                    {v.isPublished || v.isPublished ? undefined : 
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Delete', clickHandler: e => this.onDeleteVersionClick(e, v), icon: <DeleteIcon/>},
                          {text: 'Revert', clickHandler: e => this.onRevertVersionClick(e, v), icon: <RevertIcon/>},
                        ]}
                      />
                    }
                  </td>
                </tr>
              );
            }
          )}
          </tbody>
        </table>
      </div>
    );
  }
}
