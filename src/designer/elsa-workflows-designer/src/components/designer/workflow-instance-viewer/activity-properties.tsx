import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import WorkflowEditorTunnel from '../state';
import {
  Activity,
  ActivityDescriptor,
  DefaultActions, InputDescriptor,
  RenderActivityPropContext,
  RenderActivityPropsContext,
  TabChangedArgs,
  TabDefinition
} from '../../../models';
import {InputDriverRegistry} from "../../../services";
import {Container} from "typedi";
import {ActivityInputContext} from "../../../services/node-input-driver";
import {FormEntry} from "../../shared/forms/form-entry";

@Component({
  tag: 'elsa-activity-properties',
})
export class ActivityProperties {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderActivityPropsContext;

  constructor() {
  }

  @Prop({mutable: true}) public activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop({mutable: true}) public activity?: Activity;

  @State() private selectedTabIndex: number = 0;

  @Method()
  public async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  public async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  public render() {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();

    const propertiesTab: TabDefinition = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab()
    };

    const commonTab: TabDefinition = {
      displayText: 'Common',
      content: () => this.renderCommonTab()
    };

    const tabs = !!activityDescriptor ? [propertiesTab, commonTab] : [];
    const mainTitle = activity.id;
    const subTitle = activityDescriptor.displayName;

    return (
      <elsa-form-panel
        mainTitle={mainTitle}
        subTitle={subTitle}
        tabs={tabs}
        selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
      />
    );
  }

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? this.activityDescriptors.find(x => x.activityType == this.activity.typeName) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private renderPropertiesTab = () => {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const properties = activityDescriptor.inputProperties;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const key = `${activityId}`;

    return <div key={key}>
      <FormEntry fieldId="ActivityId" label="ID" hint="The ID of the activity.">
        <input type="text" name="ActivityId" id="ActivityId" value={activityId}/>
      </FormEntry>

      <FormEntry fieldId="ActivityDisplayText" label="Display Text" hint="The text to display on the activity in the designer.">
        <input type="text" name="ActivityDisplayText" id="ActivityDisplayText" value={displayText}/>
      </FormEntry>

      {properties.map(property => {
        const key = `${activity.id}-${property.name}`;
        return <div key={key}>
          {activity[property.name]}
        </div>;
      })}
    </div>
  };

  private renderCommonTab = () => {
    return <div>
    </div>
  };
}

WorkflowEditorTunnel.injectProps(ActivityProperties, ['activityDescriptors']);
