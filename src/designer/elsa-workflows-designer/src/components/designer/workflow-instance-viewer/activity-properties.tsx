import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import WorkflowEditorTunnel from '../state';
import {
  Activity,
  ActivityDescriptor,
  Lookup,
  RenderActivityPropsContext,
  TabChangedArgs,
  TabDefinition
} from '../../../models';
import {InfoList} from "../../shared/forms/info-list";
import descriptorsStore from "../../../data/descriptors-store";

@Component({
  tag: 'elsa-activity-properties',
})
export class ActivityProperties {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderActivityPropsContext;

  constructor() {
  }

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

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? descriptorsStore.activityDescriptors.find(x => x.activityType == this.activity.typeName) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private renderPropertiesTab = () => {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const properties = activityDescriptor.inputProperties;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';

    const propertyDetails: Lookup<string> = {
      'Activity ID': activityId,
      'Display Text': displayText
    };

    for (const property of properties) {
      const propertyName = camelCase(property.name);
      const propertyValue = activity[propertyName]?.expression?.value;
      const propertyValueText = propertyValue != null ? propertyValue.toString() : '';
      propertyDetails[property.displayName] = propertyValueText;
    }

    return <div>
      <InfoList title="Properties" dictionary={propertyDetails}/>
    </div>
  };

  private renderCommonTab = () => {
    return <div>
    </div>
  };
}

WorkflowEditorTunnel.injectProps(ActivityProperties, ['activityDescriptors']);
