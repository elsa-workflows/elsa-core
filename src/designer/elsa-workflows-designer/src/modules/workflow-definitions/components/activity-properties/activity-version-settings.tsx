import {h, Component, Prop, State} from "@stencil/core";
import {CheckboxFormEntry, FormEntry} from "../../../../components/shared/forms/form-entry";
import {Activity} from "../../../../models";
import {RenderActivityPropsContext, WorkflowDefinitionActivity} from "../models";

@Component({
  tag: 'elsa-workflow-definition-activity-version-settings',
  shadow: false
})
export class WorkflowDefinitionActivityVersionSettings {

  @Prop() renderContext: RenderActivityPropsContext;
  @State() private alwaysUsePublishedVersion: boolean;

  componentWillLoad(){
    const activity = this.renderContext.activity as WorkflowDefinitionActivity;
    this.alwaysUsePublishedVersion = activity.alwaysUsePublishedVersion;
  }

  render() {
    const activity = this.renderContext.activity as WorkflowDefinitionActivity;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const alwaysUsePublishedVersion = activity.alwaysUsePublishedVersion;
    const useVersion = activity.useVersion;
    const key = `${activityId}`;

    return <div key={key}>
      <FormEntry fieldId="Version" label="Version" hint="The ID of the activity.">
        <select name="Version" id="Version" onChange={e => this.onVersionChanged(e)} disabled={alwaysUsePublishedVersion}>
          <option>1</option>
          <option>2</option>
          <option>3</option>
        </select>
      </FormEntry>

      <CheckboxFormEntry fieldId="AlwaysUsePublishedVersion" label="Use published version" hint="When checked, this activity will always use its published version.">
        <input type="checkbox" name="AlwaysUsePublishedVersion" id="AlwaysUsePublishedVersion" value={"true"} checked={alwaysUsePublishedVersion} onChange={e => this.onAlwaysUsePublishedVersionChanged(e)}/>
      </CheckboxFormEntry>

    </div>
  }

  private onAlwaysUsePublishedVersionChanged(e: any) {
    const activity = this.renderContext.activity as WorkflowDefinitionActivity;
    const element = e.target as HTMLInputElement;
    const checked = element.checked;
    this.alwaysUsePublishedVersion = checked;
    activity.alwaysUsePublishedVersion = checked;
    //this.updateActivity();
  }

  private onVersionChanged(e: any) {
    const activity: Activity = this.renderContext.activity;
    const element = e.target as HTMLSelectElement;
    const version = parseInt(element.value);

    //this.updateActivity();
  }
}
