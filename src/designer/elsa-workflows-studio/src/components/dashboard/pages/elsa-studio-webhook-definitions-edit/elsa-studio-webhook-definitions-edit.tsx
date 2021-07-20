import {Component, Prop, h} from '@stencil/core';
import {MatchResults} from '@stencil/router';

@Component({
  tag: 'elsa-studio-webhook-definitions-edit',
  shadow: false,
})
export class ElsaStudioWebhookDefinitionsEdit {
  @Prop() match: MatchResults;

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
        <elsa-webhook-definition-editor-screen webhook-definition-id={id}/>
      </div>
    )
  }
}
