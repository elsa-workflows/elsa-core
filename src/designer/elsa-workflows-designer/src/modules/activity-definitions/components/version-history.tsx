import {Component, Event, EventEmitter, h, Prop, Watch} from '@stencil/core';
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {ActivityDefinition} from "../models";
import {ActivityDefinitionsApi} from "../services/api";
import {DeleteIcon, RevertIcon, PublishedIcon} from "../../../components/icons/tooling";
import moment from "moment";
import {ModalDialogService, DefaultModalActions, DefaultContents, ModalType} from "../../../components/shared/modal-dialog";

@Component({
  tag: 'elsa-activity-definition-version-history',
})
export class WorkflowDefinitionVersionHistory {
  private readonly eventBus: EventBus;
  private readonly activityDefinitionApi: ActivityDefinitionsApi;
  private readonly modalDialogService: ModalDialogService;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.activityDefinitionApi = Container.get(ActivityDefinitionsApi);
    this.modalDialogService = Container.get(ModalDialogService);
  }

  @Prop() selectedVersion: ActivityDefinition;
  @Prop() activityVersions: Array<ActivityDefinition>;
  @Prop() serverUrl: string;
  @Event() versionSelected: EventEmitter<ActivityDefinition>;
  @Event() deleteVersionClicked: EventEmitter<ActivityDefinition>;
  @Event() revertVersionClicked: EventEmitter<ActivityDefinition>;

  onViewVersionClick = (e: Event, version: ActivityDefinition) => {
    e.preventDefault();
    this.versionSelected.emit(version);
  };

  onDeleteVersionClick = async (e: Event, version: ActivityDefinition) => {
    e.preventDefault();
    this.modalDialogService.show(
      () => DefaultContents.Warning("Are you sure you want to delete this version?"),
      {
        modalType: ModalType.Confirmation,
        actions: [DefaultModalActions.Delete(() => this.deleteVersionClicked.emit(version)), DefaultModalActions.Cancel()]
      });
  };

  onRevertVersionClick = (e: Event, version: ActivityDefinition) => {
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
          {this.activityVersions.map(v => {
              let menuItems = [];
              menuItems.push({text: 'Delete', handler: e => this.onDeleteVersionClick(e, v), icon: <DeleteIcon/>});

              if (!v.isLatest)
                menuItems.push({text: 'Revert', handler: e => this.onRevertVersionClick(e, v), icon: <RevertIcon/>});

              return (
                <tr>
                  <td>{v.isPublished ? <PublishedIcon/> : ""}</td>
                  <td>{v.version}</td>
                  <td>{moment(v.createdAt).format('DD-MM-YYYY HH:mm:ss')}</td>
                  <td>
                    <button onClick={e => this.onViewVersionClick(e, v)}
                            type="button"
                            disabled={this.selectedVersion.version == v.version}
                            class={this.selectedVersion.version == v.version ? "btn btn-primary" : "btn btn-secondary"}>
                      View
                    </button>
                  </td>
                  <td>
                    {v.isPublished || v.isPublished ? undefined : <elsa-context-menu menuItems={menuItems}/>}
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

