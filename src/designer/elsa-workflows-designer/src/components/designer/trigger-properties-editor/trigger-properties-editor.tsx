import {Component, Event, EventEmitter, h, Method, Prop, State} from '@stencil/core';
import {camelCase} from 'lodash';
import WorkflowEditorTunnel from '../state';
import {
  DefaultActions, InputDescriptor,
  RenderNodePropContext,
  RenderNodePropsContext,
  TabChangedArgs,
  TabDefinition,
  Trigger,
  TriggerDescriptor
} from '../../../models';
import {FormEntry} from "../../shared/forms/form-entry";
import {InputDriverRegistry} from "../../../services/input-driver-registry";
import {Container} from "typedi";
import {NodeInputContext} from "../../../services/node-input-driver";

export interface TriggerUpdatedArgs {
  trigger: Trigger;
}

export interface DeleteTriggerRequestedArgs {
  trigger: Trigger;
}

@Component({
  tag: 'elsa-trigger-properties-editor',
})
export class TriggerPropertiesEditor {
  private slideOverPanel: HTMLElsaSlideOverPanelElement;
  private renderContext: RenderNodePropsContext;
  private readonly inputDriverRegistry: InputDriverRegistry;

  constructor() {
    this.inputDriverRegistry = Container.get(InputDriverRegistry);
  }

  @Prop({mutable: true}) triggerDescriptors: Array<TriggerDescriptor> = [];
  @Prop({mutable: true}) trigger?: Trigger;

  @Event() triggerUpdated: EventEmitter<TriggerUpdatedArgs>;
  @Event() deleteTriggerRequested: EventEmitter<DeleteTriggerRequestedArgs>;

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
    const trigger = this.trigger;
    const triggerDescriptor = this.findDescriptor();
    const title = triggerDescriptor?.displayName ?? triggerDescriptor?.nodeType ?? 'Unknown Trigger';
    const driverRegistry = this.inputDriverRegistry;

    const renderPropertyContexts: Array<RenderNodePropContext> = triggerDescriptor.inputProperties.map(inputDescriptor => {
      const renderInputContext: NodeInputContext = {
        node: trigger,
        nodeDescriptor: triggerDescriptor,
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
      node: trigger,
      nodeDescriptor: triggerDescriptor,
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
    const actions = [DefaultActions.Delete(this.onDeleteTrigger)];

    return (
      <elsa-form-panel
        headerText={title} tabs={tabs} selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}
        actions={actions}/>
    );
  }

  private findDescriptor = (): TriggerDescriptor => !!this.trigger ? this.triggerDescriptors.find(x => x.nodeType == this.trigger.nodeType) : null;

  private onSelectedTabIndexChanged(e: CustomEvent<TabChangedArgs>) {
    this.selectedTabIndex = e.detail.selectedTabIndex;
  }

  private onTriggerIdChanged = (e: any) => {
    const trigger = this.trigger;
    const inputElement = e.target as HTMLInputElement;

    trigger.id = inputElement.value;
    this.triggerUpdated.emit({trigger});
  };

  private onPropertyEditorChanged = (inputDescriptor: InputDescriptor, propertyValue: any, syntax: string) => {
    const trigger = this.trigger;
    const propertyName = inputDescriptor.name;
    const camelCasePropertyName = camelCase(propertyName);

    trigger[camelCasePropertyName] = {
      type: inputDescriptor.type,
      expression: {
        type: syntax,
        value: propertyValue
      }
    };

    this.triggerUpdated.emit({trigger});
  }

  private onDeleteTrigger = () => this.deleteTriggerRequested.emit({trigger: this.trigger});

  private renderPropertiesTab = () => {
    const {node, properties} = this.renderContext;
    const activityId = node.id;

    return <div>
      <FormEntry fieldId="ActivityId" label="ID" hint="The ID of the activity.">
        <input type="text" name="ActivityId" id="ActivityId" value={activityId} onChange={e => this.onTriggerIdChanged(e)}/>
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

WorkflowEditorTunnel.injectProps(TriggerPropertiesEditor, ['triggerDescriptors']);
