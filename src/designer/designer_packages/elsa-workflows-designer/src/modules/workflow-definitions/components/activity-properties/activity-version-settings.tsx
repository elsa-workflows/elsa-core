import {h, Component, Prop, State, Watch} from "@stencil/core";
import {RenderActivityPropsContext, WorkflowDefinitionActivity} from "../models";
import {isNullOrWhitespace} from "../../../../utils";
import {InfoList} from "../../../../components/shared/forms/info-list";

@Component({
  tag: 'elsa-workflow-definition-activity-version-settings',
  shadow: false
})
export class WorkflowDefinitionActivityVersionSettings {

  @Prop() renderContext: RenderActivityPropsContext;

  @State() private currentVersion: number;

  @Watch('renderContext')
  onRenderContextChanged(value: RenderActivityPropsContext) {
    const activity = value?.activity as WorkflowDefinitionActivity;
    this.currentVersion = activity?.version ?? 0;
  }

  componentWillLoad() {
    this.onRenderContextChanged(this.renderContext);
  }

  render() {

    if (!this.renderContext)
      return <div/>;

    const activity = this.renderContext.activity as WorkflowDefinitionActivity;
    const activityId = activity.id;
    const latestAvailablePublishedVersion = activity.latestAvailablePublishedVersion;
    const upgradeAvailable = activity.version < latestAvailablePublishedVersion;
    const key = `${activityId}`;

    const versionDetails = {
      'Current version': this.currentVersion.toString(),
      'Published version': latestAvailablePublishedVersion?.toString() ?? '-',
    };

    return <div key={key}>
      <InfoList title="Version" dictionary={versionDetails}/>
      {upgradeAvailable &&
        <div>
          <button class="btn btn-default" onClick={e => this.onUpdateClick()}>Upgrade</button>
        </div>}
    </div>
  }

  private onUpdateClick = () => {
    const activity = this.renderContext.activity as WorkflowDefinitionActivity;
    activity.version = activity.latestAvailablePublishedVersion;
    activity.workflowDefinitionVersionId = activity.latestAvailablePublishedVersionId;
    this.currentVersion = activity.version;
    this.renderContext.notifyActivityChanged();
  };
}
