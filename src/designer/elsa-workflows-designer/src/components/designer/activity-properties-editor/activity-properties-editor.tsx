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

export interface ActivityUpdatedArgs {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  propertyName?: string;
  inputDescriptor?: InputDescriptor;
}

export interface DeleteActivityRequestedArgs {
  activity: Activity;
}

@Component({
  tag: 'elsa-activity-properties-editor',
})
export class ActivityPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderActivityPropsContext;
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
    const title = activityDescriptor?.displayName ?? activityDescriptor?.activityType ?? 'Unknown Activity';
    const driverRegistry = this.inputDriverRegistry;

    const renderPropertyContexts: Array<RenderActivityPropContext> = activityDescriptor.inputProperties.map(inputDescriptor => {
      const renderInputContext: ActivityInputContext = {
        node: activity,
        nodeDescriptor: activityDescriptor,
        inputDescriptor,
        notifyInputChanged: () => this.activityUpdated.emit({activity, activityDescriptor, propertyName: camelCase(inputDescriptor.name), inputDescriptor}),
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
      activity,
      activityDescriptor,
      title,
      properties: renderPropertyContexts
    }
  }

  public render() {
    const {activityDescriptor, title} = this.renderContext;

    const propertiesTab: TabDefinition = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab()
    };

    const commonTab: TabDefinition = {
      displayText: 'Common',
      content: () => this.renderCommonTab()
    };

    const tabs = !!activityDescriptor ? [propertiesTab, commonTab] : [];
    const actions = [DefaultActions.Delete(this.onDeleteActivity)];

    return (
      <elsa-form-panel
        headerText={title} tabs={tabs} selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
        actions={actions}/>
    );
  }

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? this.activityDescriptors.find(x => x.activityType == this.activity.typeName) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private onActivityIdChanged = (e: any) => {
    const activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.id = inputElement.value;
    const activityDescriptor = this.findActivityDescriptor();
    const inputDescriptor: InputDescriptor = {
      name: 'Id',
      displayName: 'Id',
      type: 'string'
    };
    this.activityUpdated.emit({activity, activityDescriptor, propertyName: 'id', inputDescriptor});
  }

  private onActivityDisplayTextChanged(e: any){
    const activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.metadata = {
      ...activity.metadata,
      displayText: inputElement.value
    };

    const activityDescriptor = this.findActivityDescriptor();
    this.activityUpdated.emit({activity, activityDescriptor});
  }

  private onPropertyEditorChanged = (inputDescriptor: InputDescriptor, propertyValue: any, syntax: string) => {
    const activity = this.activity;
    const propertyName = inputDescriptor.name;
    const activityDescriptor = this.findActivityDescriptor();
    const camelCasePropertyName = camelCase(propertyName);

    activity[camelCasePropertyName] = {
      type: inputDescriptor.type,
      expression: {
        type: syntax,
        value: propertyValue // TODO: The "value" field is currently hardcoded, but we should be able to be more flexible and potentially have different fields for a given syntax.
      }
    };

    this.activityUpdated.emit({activity, activityDescriptor, propertyName: camelCasePropertyName, inputDescriptor});
  }

  private onDeleteActivity = () => this.deleteActivityRequested.emit({activity: this.activity});

  private renderPropertiesTab = () => {
    const {activity, properties} = this.renderContext;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const key = `${activityId}`;

    return <div key={key}>
      <FormEntry fieldId="ActivityId" label="ID" hint="The ID of the activity.">
        <input type="text" name="ActivityId" id="ActivityId" value={activityId} onChange={e => this.onActivityIdChanged(e)}/>
      </FormEntry>

      <FormEntry fieldId="ActivityDisplayText" label="Display Text" hint="The text to display on the activity in the designer.">
        <input type="text" name="ActivityDisplayText" id="ActivityDisplayText" value={displayText} onChange={e => this.onActivityDisplayTextChanged(e)}/>
      </FormEntry>

      {properties.filter(x => !!x.inputControl).map(propertyContext => {
        const key = `${activity.id}-${propertyContext.inputContext.inputDescriptor.name}`;
        return <div key={key}>
          {propertyContext.inputControl}
        </div>;
      })}
    </div>
  };

  private renderCommonTab = () => {
    return <div>
    </div>
  };
}

WorkflowEditorTunnel.injectProps(ActivityPropertiesEditor, ['activityDescriptors']);
