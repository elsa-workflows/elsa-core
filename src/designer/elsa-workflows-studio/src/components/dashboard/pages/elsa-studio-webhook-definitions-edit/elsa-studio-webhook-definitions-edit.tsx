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
        <elsa-webhook-definition-editor-screen history={this.history} server-url={this.serverUrl} webhook-definition-id={id}/>
      </div>
    )
  }
}
