import {Component, h, Host, Method, State} from '@stencil/core';
import {DefaultActions, EventTypes, Label} from "../../../models";
import {Container} from "typedi";
import labelStore from "../../../data/label-store";
import {ElsaApiClientProvider, ElsaClient, EventBus} from "../../../services";
import {CreateLabelEventArgs, DeleteLabelEventArgs, UpdateLabelEventArgs} from "./models";

@Component({
  tag: 'elsa-labels-manager',
  shadow: false,
})
export class LabelsManager {
  private elsaClient: ElsaClient;
  private modalDialog: HTMLElsaModalDialogElement;
  private eventBus: EventBus;

  @State() private createMode: boolean = false;

  @Method()
  public async show() {
    await this.modalDialog.show();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  public async componentWillLoad() {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
    this.eventBus = Container.get(EventBus);
  }

  render() {
    const labels = labelStore.labels;
    const createMode = this.createMode;
    const closeAction = DefaultActions.Close();
    const actions = [closeAction];

    return (
      <Host class="block">

        <elsa-modal-dialog ref={el => this.modalDialog = el} actions={actions}>
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Labels</h2>

            <div class="pl-4 pr-6">
              <div class="flex justify-end">
                <div class="flex-shrink">
                  <button type="button" class="btn" onClick={() => this.createMode = !this.createMode}>New label</button>
                </div>
              </div>

              {createMode ? <elsa-label-creator onCreateLabelClicked={e => this.onCreateLabelClicked(e)}/> : undefined}

              <div class="mt-5">
                <div class="flex">
                  <div>
                    <p class="max-w-2xl text-sm text-gray-500">{labels.length == 1 ? '1 label' : `${labels.length} labels`}</p>
                  </div>
                </div>
                <div class="mt-5 border-t border-gray-200">
                  <div class="divide-y divide-gray-200">
                    {labels.map(label => <div class="border-top last:border-bottom border-solid border-gray-200">
                      <elsa-label-editor key={label.id}
                                         label={label}
                                         onLabelUpdated={e => this.onLabelUpdated(e)}
                                         onLabelDeleted={e => this.onLabelDeleted(e)}
                      />
                    </div>)}
                  </div>
                </div>
              </div>

            </div>
          </div>
        </elsa-modal-dialog>
      </Host>
    );
  }

  private createLabel = async (name: string, description?: string, color?: string): Promise<void> => {
    await this.elsaClient.labels.create(name, description, color);
  }

  private updateLabel = async (id: string, name: string, description?: string, color?: string): Promise<void> => {
    await this.elsaClient.labels.update(id, name, description, color);
  }

  private deleteLabel = async (id: string): Promise<void> => {
    await this.elsaClient.labels.delete(id);
  }

  private async loadLabels() {
    const elsaClient = this.elsaClient;
    labelStore.labels = await elsaClient.labels.list();
  }

  private async refreshLabels() {
    await this.loadLabels();
    await this.eventBus.emit(EventTypes.Labels.Updated, this);
  }

  private async onCreateLabelClicked(e: CustomEvent<CreateLabelEventArgs>) {
    const args = e.detail;
    this.createMode = false;
    await this.createLabel(args.name, args.description, args.color);
    await this.refreshLabels();
  }

  private onLabelDeleted = async (e: CustomEvent<DeleteLabelEventArgs>) => {
    await this.deleteLabel(e.detail.id);
    await this.refreshLabels();
  }

  private onLabelUpdated = async (e: CustomEvent<UpdateLabelEventArgs>) => {
    const {id, name, description, color} = e.detail;
    await this.updateLabel(id, name, description, color);
    await this.refreshLabels();
  }
}

