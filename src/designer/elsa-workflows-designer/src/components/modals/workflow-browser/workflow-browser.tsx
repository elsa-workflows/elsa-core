import {Component, Event, EventEmitter, h, Host, Method, Prop, State, Watch} from '@stencil/core';
import {DefaultActions, PagedList, VersionOptions, WorkflowSummary} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider, ElsaClient} from "../../../services";
import {DeleteIcon, EditIcon, PublishIcon, UnPublishIcon} from "../../icons/tooling";

@Component({
  tag: 'elsa-workflow-browser',
  shadow: false,
})
export class WorkflowBrowser {
  private elsaClient: ElsaClient;
  private modalDialog: HTMLElsaModalDialogElement;

  @Event() public workflowDefinitionSelected: EventEmitter<WorkflowSummary>;
  @State() private workflowDefinitions: PagedList<WorkflowSummary> = {items: [], totalCount: 0};
  @State() private publishedWorkflowDefinitions: Array<WorkflowSummary> = [];

  @Method()
  public async show() {
    await this.modalDialog.show();
    await this.loadWorkflowDefinitions();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  public async componentWillLoad() {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
  }

  private async onPublishClick(e: MouseEvent, workflowDefinition: WorkflowSummary) {
    // const elsaClient = await this.createClient();
    // await elsaClient.workflowDefinitionsApi.publish(workflowDefinition.definitionId);
    // await this.loadWorkflowDefinitions();
  }

  private async onUnPublishClick(e: MouseEvent, workflowDefinition: WorkflowSummary) {
    // const elsaClient = await this.createClient();
    // await elsaClient.workflowDefinitionsApi.retract(workflowDefinition.definitionId);
    // await this.loadWorkflowDefinitions();
  }

  private async onDeleteClick(e: MouseEvent, workflowDefinition: WorkflowSummary) {

    // const result = await this.confirmDialog.show(t('DeleteConfirmationModel.Title'), t('DeleteConfirmationModel.Message'));
    //
    // if (!result)
    //   return;
    //
    // const elsaClient = await this.createClient();
    // await elsaClient.workflowDefinitionsApi.delete(workflowDefinition.definitionId);
    // await this.loadWorkflowDefinitions();
  }

  private onWorkflowDefinitionClick = async (e: MouseEvent, workflowDefinition: WorkflowSummary) => {
    e.preventDefault();
    this.workflowDefinitionSelected.emit(workflowDefinition);
    await this.hide();
  }

  private async loadWorkflowDefinitions() {
    const elsaClient = this.elsaClient;
    const page = 0;
    const pageSize = 50;
    const latestVersionOptions: VersionOptions = {isLatest: true};
    const publishedVersionOptions: VersionOptions = {isPublished: true};
    const latestWorkflowDefinitions = await elsaClient.workflows.list({page: page, pageSize: pageSize, versionOptions: {isLatest: true}});
    const unpublishedWorkflowDefinitionIds = latestWorkflowDefinitions.items.filter(x => !x.isPublished).map(x => x.definitionId);
    this.publishedWorkflowDefinitions = await elsaClient.workflows.getMany({definitionIds: unpublishedWorkflowDefinitionIds, versionOptions: publishedVersionOptions});
    this.workflowDefinitions = latestWorkflowDefinitions;
  }

  render() {

    const workflowDefinitions = this.workflowDefinitions;
    const publishedWorkflowDefinitions = this.publishedWorkflowDefinitions;
    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

    return (
      <Host class="block">

        <elsa-modal-dialog ref={el => this.modalDialog = el} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Workflow Definitions</h2>
            <div class="align-middle inline-block min-w-full border-b border-gray-200">
              <table>
                <thead>
                <tr>
                  <th><span class="lg:pl-2">Name</span></th>
                  <th>Instances</th>
                  <th class="optional align-right">Latest Version</th>
                  <th class="optional align-right">Published Version</th>
                  <th/>
                </tr>
                </thead>
                <tbody>
                {workflowDefinitions.items.map(workflowDefinition => {
                  const latestVersionNumber = workflowDefinition.version;
                  const {isPublished} = workflowDefinition;
                  const publishedVersion: WorkflowSummary = isPublished ? workflowDefinition : publishedWorkflowDefinitions.find(x => x.definitionId == workflowDefinition.definitionId);
                  const publishedVersionNumber = !!publishedVersion ? publishedVersion.version : '-';
                  let workflowDisplayName = workflowDefinition.name;

                  if (!workflowDisplayName || workflowDisplayName.trim().length == 0)
                    workflowDisplayName = 'Untitled';

                  return (
                    <tr>
                      <td>
                        <div class="flex items-center space-x-3 lg:pl-2">
                          <a onClick={e => this.onWorkflowDefinitionClick(e, workflowDefinition)} href="#" class="truncate hover:text-gray-600"><span>{workflowDisplayName}</span></a>
                        </div>
                      </td>

                      <td>
                        <div class="flex items-center space-x-3 lg:pl-2">
                          <a href="#" class="truncate hover:text-gray-600">Instances</a>
                        </div>
                      </td>

                      <td class="optional align-right">{latestVersionNumber}</td>
                      <td class="optional align-right">{publishedVersionNumber}</td>
                      <td class="pr-6">
                        <elsa-context-menu menuItems={[
                          {text: 'Edit', clickHandler: e => this.onWorkflowDefinitionClick(e, workflowDefinition), icon: <EditIcon/>},
                          isPublished ? {text: 'Unpublish', clickHandler: e => this.onUnPublishClick(e, workflowDefinition), icon: <UnPublishIcon/>} : {
                            text: 'Publish',
                            clickHandler: e => this.onPublishClick(e, workflowDefinition),
                            icon: <PublishIcon/>
                          },
                          {text: 'Delete', clickHandler: e => this.onDeleteClick(e, workflowDefinition), icon: <DeleteIcon/>}
                        ]}/>
                      </td>
                    </tr>
                  );
                })}
                </tbody>
              </table>
            </div>

            {/*<confirm-dialog ref={el => this.confirmDialog = el} culture={this.culture}/>*/}
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }
}
