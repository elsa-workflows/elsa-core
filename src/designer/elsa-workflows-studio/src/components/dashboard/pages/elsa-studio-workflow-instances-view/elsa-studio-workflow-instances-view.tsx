import {Component, Prop, h} from '@stencil/core';
import {MatchResults} from '@stencil/router';

@Component({
  tag: 'elsa-studio-workflow-instances-view',
  shadow: false,
})
export class ElsaStudioWorkflowInstancesView {
  @Prop() match: MatchResults;

  id?: string;

  componentWillLoad() {
    this.id = this.match.params.id;
  }

  render() {
    const id = this.id;

    return <div>
      <elsa-workflow-instance-viewer-screen workflowInstanceId={id}/>
    </div>;
  }
}
