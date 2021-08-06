import {Component, Prop, h} from '@stencil/core';
import {MatchResults} from '@stencil/router';

@Component({
  tag: 'elsa-studio-workflow-blueprint-view',
  shadow: false,
})
export class ElsaStudioWorkflowBlueprintView {
  @Prop() match: MatchResults;

  id?: string;

  componentWillLoad() {
    this.id = this.match.params.id;
  }

  render() {
    const id = this.id;

    return <div>
      <elsa-workflow-blueprint-viewer-screen workflowDefinitionId={id}/>
    </div>;
  }
}
