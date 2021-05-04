import {Component, Prop, h} from '@stencil/core';
import {MatchResults} from '@stencil/router';

@Component({
  tag: 'elsa-studio-workflow-definitions-edit',
  shadow: false,
})
export class ElsaStudioWorkflowDefinitionsEdit {
  @Prop() match: MatchResults;
  @Prop() serverUrl: string;
  @Prop() monacoLibPath: string;

  id?: string;

  componentWillLoad() {
    let id = this.match.params.id;

    if (!!id && id.toLowerCase() == 'new')
      id = null;

    this.id = id;
  }

  render() {
    const id = this.id;

    return <div>
      <elsa-workflow-definition-editor-screen server-url={this.serverUrl} workflow-definition-id={id} monaco-lib-path={this.monacoLibPath}/>
    </div>;
  }
}
