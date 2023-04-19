import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import {v4 as uuid} from 'uuid';
import {Activity, ActivityDescriptor, ActivityInput, ActivityKind, ActivityOutput, Expression, InputDescriptor, OutputDescriptor, PropertyDescriptor, TabChangedArgs, TabDefinition, Variable} from '../../../../models';
import {EventBus, InputDriverRegistry} from "../../../../services";
import {Container} from "typedi";
import {ActivityInputContext} from "../../../../services/activity-input-driver";
import {CheckboxFormEntry, FormEntry} from "../../../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../../utils";
import descriptorsStore from "../../../../data/descriptors-store";
import {ActivityPropertyPanelEvents, ActivityUpdatedArgs, DeleteActivityRequestedArgs} from "../../models/ui";
import InputControlSwitchContextState from "../../../../components/shared/input-control-switch/state";
import {OutputDefinition} from "../../models/entities";
import {RenderActivityInputContext, RenderActivityPropsContext} from "../models";

@Component({
  tag: 'elsa-activity-properties-editor',
})
export class ActivityPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderActivityPropsContext;
  private readonly inputDriverRegistry: InputDriverRegistry;
  private readonly eventBus: EventBus;

  constructor() {
    this.inputDriverRegistry = Container.get(InputDriverRegistry);
    this.eventBus = Container.get(EventBus);
  }

  @Prop() workflowDefinitionId: string;
  @Prop() activity?: Activity;
  @Prop() variables: Array<Variable> = [];
  @Prop() outputs: Array<OutputDefinition> = [];

  @Event() activityUpdated: EventEmitter<ActivityUpdatedArgs>;
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

  async componentWillRender() {
    const activity = this.activity;
    const activityDescriptor = this.findActivityDescriptor();
    const title = activityDescriptor?.displayName ?? activityDescriptor?.typeName ?? 'Unknown Activity';
    const inputs = this.createInputs();
    const tabs = this.createTabs();

    const onActivityChanged = () => this.activityUpdated.emit({
      activity,
      activityDescriptor
    });

    this.renderContext = {
      activity,
      activityDescriptor,
      title,
      inputs,
      tabs,
      notifyActivityChanged: () => onActivityChanged()
    }

    await this.eventBus.emit(ActivityPropertyPanelEvents.Displaying, this, this.renderContext);
  }

  render() {
    const {activity, activityDescriptor, tabs} = this.renderContext;
    const actions = [];
    const mainTitle = activity.id;
    const subTitle = activityDescriptor.displayName;
    const selectedTabIndex = this.getSelectedTabIndex(tabs);

    return (
      <elsa-form-panel
        mainTitle={mainTitle}
        subTitle={subTitle}
        orientation="Landscape"
        tabs={tabs}
        selectedTabIndex={selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
        actions={actions}/>
    );
  }

  private getSelectedTabIndex = (tabs: Array<TabDefinition>): number => {
    let selectedTabIndex = this.selectedTabIndex;

    if (selectedTabIndex >= tabs.length)
      selectedTabIndex = tabs.length - 1;

    if (selectedTabIndex < 0)
      selectedTabIndex = 0;

    return selectedTabIndex;
  };

  private createTabs = (): Array<TabDefinition> => {
    const activityDescriptor = this.findActivityDescriptor();
    const isTask = activityDescriptor.kind == ActivityKind.Task;

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

    if (isTask) {
      const taskTab: TabDefinition = {
        displayText: 'Task',
        order: 12,
        content: () => this.renderTaskTab()
      };

      tabs.push(taskTab);
    }

    return tabs;
  };

  private createInputs = (): Array<RenderActivityInputContext> => {
    const activity = this.activity;
    const activityId = activity.id;
    const activityDescriptor = this.findActivityDescriptor();
    const driverRegistry = this.inputDriverRegistry;

    const onInputChanged = (inputDescriptor: InputDescriptor) => this.activityUpdated.emit({
      newId: activityId,
      originalId: activityId,
      activity,
      activityDescriptor,
      // propertyName: camelCase(inputDescriptor.name),
      // propertyDescriptor: inputDescriptor
    });

    return activityDescriptor.inputs.map(inputDescriptor => {
      const renderInputContext: ActivityInputContext = {
        activity: activity,
        activityDescriptor: activityDescriptor,
        inputDescriptor,
        notifyInputChanged: () => onInputChanged(inputDescriptor),
        inputChanged: (v, s) => this.onInputPropertyEditorChanged(inputDescriptor, v, s)
      };

      const driver = driverRegistry.get(renderInputContext);
      const workflowDefinitionId = this.workflowDefinitionId;
      const activityType = activityDescriptor.typeName;
      const propertyName = inputDescriptor.name;

      const control =
        <InputControlSwitchContextState.Provider state={{workflowDefinitionId, activityType, propertyName}}>
          {driver?.renderInput(renderInputContext)}
        </InputControlSwitchContextState.Provider>;

      return {
        inputContext: renderInputContext,
        inputControl: control,
      }
    });
  };

  private findActivityDescriptor = (): ActivityDescriptor => !!this.activity ? descriptorsStore.activityDescriptors.find(x => x.typeName == this.activity.type && x.version == this.activity.version) : null;
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
      typeName: 'String'
    };

    activity.id = newId;

    this.activityUpdated.emit({
      newId: newId,
      originalId: originalId,
      activity,
      activityDescriptor,
      // propertyName: 'id',
      // propertyDescriptor: inputDescriptor
    });
  }

  private onActivityDisplayTextChanged(e: any) {
    const activity: Activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.metadata = {
      ...activity.metadata,
      displayText: inputElement.value
    };

    this.updateActivity();
  }

  private onCanStartWorkflowChanged(e: any) {
    const activity: Activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.canStartWorkflow = inputElement.checked;

    this.updateActivity();
  }

  private onRunAsynchronouslyChanged(e: any) {
    const activity: Activity = this.activity;
    const inputElement = e.target as HTMLInputElement;

    activity.runAsynchronously = inputElement.checked;

    this.updateActivity();
  }

  private onInputPropertyEditorChanged = (inputDescriptor: InputDescriptor, propertyValue: any, syntax: string) => {
    const activity = this.activity;
    const propertyName = inputDescriptor.name;
    const isWrapped = inputDescriptor.isWrapped;
    const camelCasePropertyName = camelCase(propertyName);

    if (isWrapped) {
      let input: ActivityInput = activity[camelCasePropertyName];

      const expression: Expression = {
        type: syntax,
        value: propertyValue // TODO: The "value" field is currently hardcoded, but we should be able to be more flexible and potentially have different fields for a given syntax.
      };

      if (!input) {
        input = {
          typeName: inputDescriptor.typeName,
          memoryReference: {id: uuid()},
          expression: expression
        }
      }

      input.expression = expression;
      activity[camelCasePropertyName] = input;

    } else {
      activity[camelCasePropertyName] = propertyValue;
    }
    this.updateActivity(propertyName);
  }

  private onOutputPropertyEditorChanged = (outputDescriptor: OutputDescriptor, outputTargetValue: string) => {
    const activity = this.activity;
    const propertyName = outputDescriptor.name;
    const camelCasePropertyName = camelCase(propertyName);
    const outputTargetValuePair = outputTargetValue.split('::');
    const outputTargetId = outputTargetValuePair[1];

    const property: ActivityOutput = {
      typeName: outputDescriptor.typeName,
      memoryReference: {
        id: outputTargetId
      }
    }

    activity[camelCasePropertyName] = property;

    this.updateActivity(propertyName);
  }

  private updateActivity = (propertyName?: string, propertyDescriptor?: PropertyDescriptor) => {
    const activityDescriptor = this.findActivityDescriptor();
    const activity = this.activity;
    const activityId = activity.id;

    this.activityUpdated.emit({
      newId: activityId,
      originalId: activityId,
      activity,
      activityDescriptor
    });
  }

  private renderCommonTab = () => {
    const {activity} = this.renderContext;
    const activityId = activity.id;
    const displayText: string = activity.metadata?.displayText ?? '';
    const canStartWorkflow: boolean = activity.canStartWorkflow;
    const key = `${activityId}`;

    return <div key={key}>
      <FormEntry fieldId="ActivityId" label="ID" hint="The ID of the activity.">
        <input type="text" name="ActivityId" id="ActivityId" value={activityId} onChange={e => this.onActivityIdChanged(e)}/>
      </FormEntry>

      <FormEntry fieldId="ActivityDisplayText" label="Display Text" hint="The text to display on the activity in the designer.">
        <input type="text" name="ActivityDisplayText" id="ActivityDisplayText" value={displayText} onChange={e => this.onActivityDisplayTextChanged(e)}/>
      </FormEntry>

      <CheckboxFormEntry fieldId="CanStartWorkflow" label="Can start workflow" hint="When enabled, this activity can be used as a trigger to automatically start the workflow.">
        <input type="checkbox" name="CanStartWorkflow" id="CanStartWorkflow" value={"true"} checked={canStartWorkflow} onChange={e => this.onCanStartWorkflowChanged(e)}/>
      </CheckboxFormEntry>

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
    const outputDefinitions = this.outputs || [];
    const variables = this.variables || [];
    const activityId = activity.id;
    const key = `${activityId}`;
    const outputTargetOptions: Array<any> = [null];

    if (variables.length > 0) {
      outputTargetOptions.push({label: 'Variables', items: [...variables.map(x => ({value: x.id, name: x.name}))], kind: 'variable'});
    }

    if (outputDefinitions.length > 0)
      outputTargetOptions.push({label: 'Outputs', items: [...outputDefinitions.map(x => ({value: x.name, name: x.name}))], kind: 'output'});

    return <div key={key}>
      {outputs.map(propertyDescriptor => {
        const key = `${activity.id}-${propertyDescriptor.name}`;
        const displayName = isNullOrWhitespace(propertyDescriptor.displayName) ? propertyDescriptor.name : propertyDescriptor.displayName;
        const propertyName = camelCase(propertyDescriptor.name);
        const propertyValue = activity[propertyName] as ActivityOutput;
        const propertyType = propertyDescriptor.typeName;
        const typeDescriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == propertyType);
        const propertyTypeName = typeDescriptor?.displayName ?? propertyType;

        return <div key={key}>
          <FormEntry fieldId={key} label={displayName} hint={propertyDescriptor.description}>

            <div class="relative">
              <select onChange={e => this.onOutputPropertyEditorChanged(propertyDescriptor, (e.currentTarget as HTMLSelectElement).value)}>
                {outputTargetOptions.map(outputTarget => {
                  if (!outputTarget) {
                    return <option value="" selected={!propertyValue?.memoryReference?.id}>-</option>
                  }

                  const items = outputTarget.items;

                  return (
                    <optgroup label={outputTarget.label}>
                      {items.map(item => {
                        const isSelected = propertyValue?.memoryReference?.id == item.value;
                        return <option value={`${outputTarget.kind}::${item.value}`} selected={isSelected}>{item.name}</option>;
                      })}
                    </optgroup>);
                })}
              </select>
              <div class="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-10">
                <span class="text-gray-500 sm:text-sm">{propertyTypeName}</span>
              </div>
            </div>
          </FormEntry>
        </div>;
      })}
    </div>
  };

  private renderTaskTab = () => {
    const {activity} = this.renderContext;
    const activityId = activity.id;
    const runAsynchronously: boolean = activity.runAsynchronously;
    const key = `${activityId}:task`;

    return <div key={key}>
      <CheckboxFormEntry fieldId="RunAsynchronously" label="Execute asynchronously" hint="When enabled, this activity will execute asynchronously and suspend workflow execution until the activity is finished.">
        <input type="checkbox" name="RunAsynchronously" id="RunAsynchronously" value={"true"} checked={runAsynchronously} onChange={e => this.onRunAsynchronouslyChanged(e)}/>
      </CheckboxFormEntry>
    </div>
  };
}
