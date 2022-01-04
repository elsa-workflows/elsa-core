import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import WorkflowEditorTunnel from '../state';
import {
  Activity,
  ActivityDescriptor,
  DefaultActions, InputDescriptor,
  RenderNodePropContext,
  RenderNodePropsContext,
  TabChangedArgs,
  TabDefinition
} from '../../../models';
import {InputDriverRegistry} from "../../../services/input-driver-registry";
import {Container} from "typedi";
import {NodeInputContext} from "../../../services/node-input-driver";
import {FormEntry} from "../../shared/forms/form-entry";

export interface ActivityUpdatedArgs {
  activity: Activity;
}

export interface DeleteActivityRequestedArgs {
  activity: Activity;
}

@Component({
  tag: 'elsa-activity-properties-editor',
})
export class ActivityPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderNodePropsContext;
  private readonly inputDriverRegistry: InputDriverRegistry;

  constructor() {
    this.inputDriverRegistry = Container.get(InputDriverRegistry);
  }

  @Prop({mutable: true}) public activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop({mutable: true}) public activity?: Activity;

  @Event() public activityUpdated: EventEmitter<ActivityUpdatedArgs>;
  @Event() public deleteActivityRequested: EventEmitter<DeleteActivityRequestedArgs>;
  @State() private selectedTabIndex: number = 0;

  @Method()
  public async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  public async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  public componentWillRender() {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const title = activityDescriptor?.displayName ?? activityDescriptor?.nodeType ?? 'Unknown Activity';
    const driverRegistry = this.inputDriverRegistry;

    const renderPropertyContexts: Array<RenderNodePropContext> = activityDescriptor.inputProperties.map(inputDescriptor => {
      const renderInputContext: NodeInputContext = {
        node: activity,
        nodeDescriptor: activityDescriptor,
        inputDescriptor,
        inputChanged: (v, s) => this.onPropertyEditorChanged(inputDescriptor, v, s)
      };

      const driver = driverRegistry.get(renderInputContext);
      const control = driver?.renderInput(renderInputContext);

      return {
        inputContext: renderInputContext,
        inputControl: control,
      }
    });

    this.renderContext = {
      node: activity,
      nodeDescriptor: activityDescriptor,
      title,
      properties: renderPropertyContexts
    }
  }

  public render() {
    const {nodeDescriptor, title} = this.renderContext;

    const propertiesTab: TabDefinition = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab()
    };

    const commonTab: TabDefinition = {
      displayText: 'Common',
      content: () => this.renderCommonTab()
    };

    const tabs = !!nodeDescriptor ? [propertiesTab, commonTab] : [];
    const actions = [DefaultActions.Delete(this.onDeleteActivity)];

    return (
      <elsa-form-panel
        headerText={title} tabs={tabs} selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
        actions={actions}/>
    );
  }

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? this.activityDescriptors.find(x => x.nodeType == this.activity.nodeType) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private onActivityIdChanged = (e: any) => {
    const activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.id = inputElement.value;
    this.activityUpdated.emit({activity: activity});
  }

  private onPropertyEditorChanged = (inputDescriptor: InputDescriptor, propertyValue: any, syntax: string) => {
    const activity = this.activity;
    const propertyName = inputDescriptor.name;
    const camelCasePropertyName = camelCase(propertyName);

    activity[camelCasePropertyName] = {
      type: inputDescriptor.type,
      expression: {
        type: syntax,
        value: propertyValue
      }
    };

    this.activityUpdated.emit({activity: activity});
  }

  private onDeleteActivity = () => this.deleteActivityRequested.emit({activity: this.activity});

  private renderPropertiesTab = () => {
    const {node, properties} = this.renderContext;
    const activityId = node.id;

    return <div>
      <FormEntry fieldId="ActivityId" label="ID" hint="The ID of the activity.">
        <input type="text" name="ActivityId" id="ActivityId" value={activityId} onChange={e => this.onActivityIdChanged(e)}/>
      </FormEntry>

      {properties.filter(x => !!x.inputControl).map(propertyContext => {
        return propertyContext.inputControl;
      })}
    </div>
  };

  private renderCommonTab = () => {
    return <div>
    </div>
  };
}

WorkflowEditorTunnel.injectProps(ActivityPropertiesEditor, ['activityDescriptors']);
