import {Component, h, Host, Method, State} from '@stencil/core';
import {DefaultActions, Label} from "../../../models";
import {Container} from "typedi";
import {ElsaApiClientProvider, ElsaClient} from "../../../services";

@Component({
  tag: 'elsa-labels-manager',
  shadow: false,
})
export class LabelsManager {
  private elsaClient: ElsaClient;
  private modalDialog: HTMLElsaModalDialogElement;

  @State() private labels: Array<Label> = [];
  @State() private createMode: boolean = false;

  @Method()
  public async show() {
    await this.modalDialog.show();
    await this.loadLabels();
  }

  @Method()
  public async hide() {
    await this.modalDialog.hide();
  }

  public async componentWillLoad() {
    const elsaClientProvider = Container.get(ElsaApiClientProvider);
    this.elsaClient = await elsaClientProvider.getClient();
  }

  private async onDeleteClick(e: MouseEvent, label: Label) {
    await this.loadLabels();
  }

  private async loadLabels() {
    const elsaClient = this.elsaClient;
    this.labels = await elsaClient.labels.list();
  }

  render() {
    const labels = this.labels;
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

              {createMode ? <elsa-label-creator /> : undefined}

              <div class="mt-5 ">
                <div class="border-l border-r border-gray-200 rounded-l-md rounded-r-md">
                  <div class="flex">
                    <div>
                      <p class="max-w-2xl text-sm text-gray-500">{labels.length == 1 ? '1 label' : `${labels.length} labels`}</p>
                    </div>
                  </div>
                </div>
                <div class="border-t border-gray-200">
                  <div class="divide-y divide-gray-200">
                    {labels.map(label => <div class="border-top last:border-bottom border-solid border-gray-200">
                      <elsa-label-editor label={label}/>
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
}

