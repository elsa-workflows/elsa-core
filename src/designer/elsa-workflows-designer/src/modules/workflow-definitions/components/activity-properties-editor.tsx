import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import {
  Activity,
  ActivityDescriptor, ActivityOutput,
  InputDescriptor, OutputDescriptor, PropertyDescriptor, RenderActivityInputContext,
  RenderActivityPropsContext,
  TabChangedArgs,
  TabDefinition, Variable
} from '../../../models';
import {InputDriverRegistry} from "../../../services";
import {Container} from "typedi";
import {ActivityInputContext} from "../../../services/node-input-driver";
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";
import descriptorsStore from "../../../data/descriptors-store";

export interface ActivityUpdatedArgs {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  propertyName?: string;
  propertyDescriptor?: PropertyDescriptor;
}

export interface ActivityIdUpdatedArgs {
  activity: Activity;
  activityDescriptor: ActivityDescriptor;
  originalId: string;
  newId: string;
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

  @Prop() activity?: Activity;
  @Prop() variables: Array<Variable> = [];

  @Event() activityUpdated: EventEmitter<ActivityUpdatedArgs>;
  @Event() activityIdUpdated: EventEmitter<ActivityIdUpdatedArgs>;
  @Event() deleteActivityRequested: EventEmitter<DeleteActivityRequestedArgs>;
  @State() private selectedTabIndex: number = 0;

  @Method()
  async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  componentWillRender() {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const title = activityDescriptor?.displayName ?? activityDescriptor?.activityType ?? 'Unknown Activity';
    const driverRegistry = this.inputDriverRegistry;

    const renderInputPropertyContexts: Array<RenderActivityInputContext> = activityDescriptor.inputs.map(inputDescriptor => {
      const renderInputContext: ActivityInputContext = {
        node: activity,
        nodeDescriptor: activityDescriptor,
        inputDescriptor,
        notifyInputChanged: () => this.activityUpdated.emit({activity, activityDescriptor, propertyName: camelCase(inputDescriptor.name), propertyDescriptor: inputDescriptor}),
        inputChanged: (v, s) => this.onInputPropertyEditorChanged(inputDescriptor, v, s)
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
      inputs: renderInputPropertyContexts
    }
  }

  render() {
    const {activity, activityDescriptor} = this.renderContext;

    const commonTab: TabDefinition = {
      displayText: 'General',
      order: 0,
      content: () => this.renderCommonTab()
    };

    const inputTab: TabDefinition = {
      displayText: 'Settings',
      order: 10,
      content: () => this.renderInputTab()
    };

    const tabs = !!activityDescriptor ? [inputTab, commonTab] : [];

    if (activityDescriptor.outputs.length > 0) {
      const outputTab: TabDefinition = {
        displayText: 'Output',
        order: 11,
        content: () => this.renderOutputTab()
      };

      tabs.push(outputTab);
    }

    let selectedTabIndex = this.selectedTabIndex;

    if (selectedTabIndex >= tabs.length)
      selectedTabIndex = tabs.length - 1;

    if (selectedTabIndex < 0)
      selectedTabIndex = 0;

    const actions = [];
    const mainTitle = activity.id;
    const subTitle = activityDescriptor.displayName;

    return (
      <elsa-form-panel
        mainTitle={mainTitle}
        subTitle={subTitle}
        tabs={tabs}
        selectedTabIndex={selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
        actions={actions}/>
    );
  }

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? descriptorsStore.activityDescriptors.find(x => x.activityType == this.activity.typeName) : null;
  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex

  private onActivityIdChanged = (e: any) => {
    const activity = this.activity;
    const originalId = activity.id;
    const inputElement = e.target as HTMLInputElement;
    const newId = inputElement.value;
    const activityDescriptor = this.findActivityDescriptor();

    const inputDescriptor: InputDescriptor = {
      name: 'Id',
      displayName: 'Id',
      type: 'string'
    };

    activity.id = newId;

    this.activityIdUpdated.emit({ activity, activityDescriptor, originalId, newId: activity.id });
    this.activityUpdated.emit({activity, activityDescriptor, propertyName: 'id', propertyDescriptor: inputDescriptor});
  }

  private onActivityDisplayTextChanged(e: any) {
    const activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.metadata = {
      ...activity.metadata,
      displayText: inputElement.value
    };

    const activityDescriptor = this.findActivityDescriptor();
    this.activityUpdated.emit({activity, activityDescriptor});
  }

  private onInputPropertyEditorChanged = (inputDescriptor: InputDescriptor, propertyValue: any, syntax: string) => {
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

    this.activityUpdated.emit({activity, activityDescriptor, propertyName: camelCasePropertyName, propertyDescriptor: inputDescriptor});
  }

  private onOutputPropertyEditorChanged = (outputDescriptor: OutputDescriptor, variableName: string) => {
    const activity = this.activity;
    const propertyName = outputDescriptor.name;
    const activityDescriptor = this.findActivityDescriptor();
    const camelCasePropertyName = camelCase(propertyName);

    const property: ActivityOutput = {
      type: outputDescriptor.type,
      memoryReference: {
        id: variableName
      }
    }

    activity[camelCasePropertyName] = property;

    this.activityUpdated.emit({activity, activityDescriptor, propertyName: camelCasePropertyName, propertyDescriptor: outputDescriptor});
  }

  private onDeleteActivity = () => this.deleteActivityRequested.emit({activity: this.activity});

  private renderCommonTab = () => {
    const {activity,} = this.renderContext;
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

    </div>
  };

  private renderInputTab = () => {
    const {activity, inputs} = this.renderContext;
    const activityId = activity.id;
    const key = `${activityId}`;

    return <div key={key}>
      {inputs.filter(x => !!x.inputControl).map(propertyContext => {
        const key = `${activity.id}-${propertyContext.inputContext.inputDescriptor.name}`;
        return <div key={key}>
          {propertyContext.inputControl}
        </div>;
      })}
    </div>
  };

  private renderOutputTab = () => {
    const {activity, activityDescriptor} = this.renderContext;
    const outputs = activityDescriptor.outputs;
    const activityId = activity.id;
    const key = `${activityId}`;
    const variableOptions: Array<any> = [null, {label: 'Variables', items: [...this.variables.map(x => ({value: x.name, name: x.name}))]}];

    return <div key={key}>
      {outputs.map(propertyDescriptor => {
        const key = `${activity.id}-${propertyDescriptor.name}`;
        const displayName = isNullOrWhitespace(propertyDescriptor.displayName) ? propertyDescriptor.name : propertyDescriptor.displayName;
        const propertyName = camelCase(propertyDescriptor.name);
        const propertyValue = activity[propertyName] as ActivityOutput;

        return <div key={key}>
          <FormEntry fieldId={key} label={displayName} hint={propertyDescriptor.description}>
            <select onChange={e => this.onOutputPropertyEditorChanged(propertyDescriptor, (e.currentTarget as HTMLSelectElement).value)}>
              {variableOptions.map(group => {
                if (!group) {
                  return <option value="" selected={!propertyValue?.memoryReference?.id}>-</option>
                }

                const items = group.items;

                return (
                  <optgroup label={group.label}>
                    {items.map(variable => {
                      const isSelected = propertyValue?.memoryReference?.id == variable.value;
                      return <option value={variable.value} selected={isSelected}>{variable.name}</option>;
                    })}
                  </optgroup>);
              })}
            </select>
          </FormEntry>
        </div>;
      })}
    </div>
  };
}
