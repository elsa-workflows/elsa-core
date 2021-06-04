import {Component, Prop, h} from '@stencil/core';
import {RouterHistory, MatchResults} from '@stencil/router';

@Component({
  tag: 'elsa-studio-webhook-definitions-edit',
  shadow: false,
})
export class ElsaStudioWebhookDefinitionsEdit {
  @Prop() match: MatchResults;
  @Prop() serverUrl: string;
  @Prop() history: RouterHistory;

  id?: string;

  componentWillLoad() {
    let id = this.match.params.id;

    if (!!id && id.toLowerCase() == 'new')
      id = null;

    this.id = id;
  }

  render() {
    const id = this.id;

    return (
      <div>
          <div class="elsa-border-b elsa-border-gray-200 elsa-px-4 elsa-py-4 sm:elsa-flex sm:elsa-items-center sm:elsa-justify-between sm:elsa-px-6 lg:elsa-px-8 elsa-bg-white">
            <div class="elsa-flex-1 elsa-min-w-0">
              <h1 class="elsa-text-lg elsa-font-medium elsa-leading-6 elsa-text-gray-900 sm:elsa-truncate">
                { null == id ? "Create Webhook Definition" : "Edit Webhook Definition" }
              </h1>
            </div>
          </div>

        <elsa-webhook-definition-editor-screen history={this.history} server-url={this.serverUrl} webhook-definition-id={id}/>
      </div>
    )
  }
}
