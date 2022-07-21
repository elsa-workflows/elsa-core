import {Component, Event, EventEmitter, h, Method, Prop, State, Watch} from '@stencil/core';
import {Container} from "typedi";
import {EventBus} from "../../../services";
import {ActivityDefinition, ActivityDefinitionPropertiesEditorModel, ActivityDefinitionPropsUpdatedArgs} from "../models";
import {PropertiesTabModel} from "../../workflow-definitions/models/ui";
import {FormEntry} from "../../../components/shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";
import {InfoList} from "../../../components/shared/forms/info-list";
import {TabModel} from "../../workflow-instances/models";
import {TabChangedArgs, Variable} from "../../../models";

@Component({
  tag: 'elsa-activity-definition-properties-editor',
})
export class Properties {
  private readonly eventBus: EventBus;
  private slideOverPanel: HTMLElsaSlideOverPanelElement;

  constructor() {
    this.eventBus = Container.get(EventBus);

    this.model = {
      tabModels: [],
    }
  }

  @Prop() activityDefinition?: ActivityDefinition;
  @Event() activityDefinitionPropsUpdated: EventEmitter<ActivityDefinitionPropsUpdatedArgs>;
  @State() private model: ActivityDefinitionPropertiesEditorModel;
  @State() private selectedTabIndex: number = 0;

  @Method()
  async show(): Promise<void> {
    await this.slideOverPanel.show();
  }

  @Method()
  async hide(): Promise<void> {
    await this.slideOverPanel.hide();
  }

  @Watch('activityDefinition')
  async onWorkflowDefinitionChanged() {
    await this.createModel();
  }

  async componentWillLoad() {
    await this.createModel();
  }

  private createModel = async () => {
    const model = {
      tabModels: []
    };

    const activityDefinition = this.activityDefinition;

    if (!activityDefinition) {
      this.model = model;
      return;
    }

    const propertiesTabModel: PropertiesTabModel = {
      name: 'properties',
      tab: null,
      Widgets: [{
        name: 'activityDefinitionTypeName',
        content: () => {
          const definition = this.activityDefinition;
          return <FormEntry label="Type Name" fieldId="activityDefinitionTypeName" hint="The type name of the custom activity.">
            <input type="text" name="activityDefinitionTypeName" id="activityDefinitionTypeName" value={definition.typeName} onChange={e => this.onPropertyEditorChanged(a => a.typeName = (e.target as HTMLInputElement).value)}/>
          </FormEntry>;
        },
        order: 0
      }, {
        name: 'activityDefinitionDisplayName',
        content: () => {
          const definition = this.activityDefinition;
          return <FormEntry label="Display Name" fieldId="activityDefinitionDisplayName" hint="The display name of the custom activity.">
            <input type="text" name="activityDefinitionDisplayName" id="activityDefinitionDisplayName" value={definition.displayName}
                   onChange={e => this.onPropertyEditorChanged(a => a.displayName = (e.target as HTMLInputElement).value)}/>
          </FormEntry>;
        },
        order: 0
      }, {
        name: 'activityDefinitionCategory',
        content: () => {
          const definition = this.activityDefinition;
          return <FormEntry label="Category" fieldId="activityDefinitionCategory" hint="The category to which this activity belongs.">
              <input type="text" name="activityDefinitionCategory" id="activityDefinitionCategory" value={definition.category}
                        onChange={e => this.onPropertyEditorChanged(a => a.category = (e.target as HTMLTextAreaElement).value)}/>
          </FormEntry>;
        },
        order: 5
      }, {
        name: 'activityDefinitionInfo',
        content: () => {
          const definition = this.activityDefinition;

          const activityDefinitionDetails = {
            'Definition ID': isNullOrWhitespace(definition.definitionId) ? '(new)' : definition.definitionId,
            'Version ID': isNullOrWhitespace(definition.id) ? '(new)' : definition.id,
            'Version': definition.version.toString(),
            'Status': definition.isPublished ? 'Published' : 'Draft'
          };

          return <InfoList title="Information" dictionary={activityDefinitionDetails}/>;
        },
        order: 10
      }]
    };

    propertiesTabModel.tab = {
      displayText: 'Properties',
      content: () => this.renderPropertiesTab(propertiesTabModel)
    };

    const variablesTabModel: TabModel = {
      name: 'variables',
      tab: {
        displayText: 'Variables',
        content: () => this.renderVariablesTab()
      }
    }

    model.tabModels = [propertiesTabModel, variablesTabModel];
    this.model = model;
  }

  private renderPropertiesTab = (tabModel: PropertiesTabModel) => {
    const widgets = tabModel.Widgets.sort((a, b) => a.order < b.order ? -1 : a.order > b.order ? 1 : 0);

    return <div>
      {widgets.map(widget => widget.content())}
    </div>
  };

  private renderVariablesTab = () => {
    const variables: Array<Variable> = this.activityDefinition?.variables ?? [];

    return <div>
      <elsa-variables-editor variables={variables} onVariablesChanged={e => this.onVariablesUpdated(e)}/>
    </div>
  };

  private onSelectedTabIndexChanged = (e: CustomEvent<TabChangedArgs>) => this.selectedTabIndex = e.detail.selectedTabIndex;

  private onPropertyEditorChanged = (apply: (w: ActivityDefinition) => void) => {
    const activityDefinition = this.activityDefinition;
    apply(activityDefinition);
    this.activityDefinitionPropsUpdated.emit({activityDefinition: activityDefinition});
  }

  private onVariablesUpdated = async (e: CustomEvent<Array<Variable>>) => {
    const activityDefinition = this.activityDefinition;

    if (!activityDefinition)
      return;

    activityDefinition.variables = e.detail;
    this.activityDefinitionPropsUpdated.emit({activityDefinition: activityDefinition});
    await this.createModel();
  }

  render() {
    const activityDefinition = this.activityDefinition;
    const title = activityDefinition?.displayName ?? activityDefinition?.typeName ?? 'Untitled';
    const subTitle = 'Activity Definition'
    const tabs = this.model.tabModels.map(x => x.tab);

    return (
      <elsa-form-panel
        mainTitle={title}
        subTitle={subTitle}
        tabs={tabs}
        selectedTabIndex={this.selectedTabIndex}
        onSelectedTabIndexChanged={e => this.onSelectedTabIndexChanged(e)}/>
    );
  }
}
