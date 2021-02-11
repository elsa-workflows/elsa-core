import {Component, h, Host, Prop} from '@stencil/core';

@Component({
  tag: 'elsa-studio',
  styleUrl: 'elsa-studio.css',
  shadow: false,
})
export class ElsaWorkflowStudio {

  @Prop({attribute: 'workflow-definition-id', reflect: true}) workflowDefinitionId: string;
  @Prop({attribute: 'server-url', reflect: true}) serverUrl: string;
  el: HTMLElement;

  render() {
    return (
      <Host class="flex flex-col" ref={el => this.el = el}>
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
