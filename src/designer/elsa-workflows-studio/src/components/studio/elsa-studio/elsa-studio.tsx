import {Component, h, Host, Prop, State, Watch} from '@stencil/core';
import {createElsaClient} from "../../../services/elsa-client";
import state from "../../../utils/store";

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {

  @Prop({attribute: 'workflow-definition-id', reflect: true}) workflowDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  el: HTMLElement;

  @Watch("serverUrl")
  async serverUrlChangedHandler(newValue: string) {
    if (newValue && newValue.length > 0)
      await this.loadActivityDescriptors();
  }

  async componentWillLoad() {
    await this.serverUrlChangedHandler(this.serverUrl);
  }

  async loadActivityDescriptors() {
    const client = createElsaClient(this.serverUrl);
    state.activityDescriptors = await client.activitiesApi.list();
  }

  render() {
    return (
      <Host class="flex flex-col w-full" ref={el => this.el = el}>
        {this.renderContentSlot()}
      </Host>
    );
  }

  renderContentSlot() {
    return (
      <div class="h-screen flex ">
        <elsa-workflow-definition-editor serverUrl={this.serverUrl} workflowDefinitionId={this.workflowDefinitionId} class="flex-1"/>
      </div>
    );
  }

}
