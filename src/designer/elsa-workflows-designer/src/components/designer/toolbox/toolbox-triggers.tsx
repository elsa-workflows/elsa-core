import {Component, h, Prop, State, Watch} from '@stencil/core';
import {Addon, Graph} from '@antv/x6';
import groupBy from 'lodash/groupBy';
import {ActivityDescriptor, TriggerDescriptor} from '../../../models';
import WorkflowEditorTunnel from '../state';
import {Container} from "typedi";
import {TriggerDriverRegistry} from '../../../services/trigger-driver-registry';

interface TriggerCategoryModel {
  category: string;
  triggers: Array<TriggerDescriptor>;
  expanded: boolean;
}

@Component({
  tag: 'elsa-toolbox-triggers',
})
export class ToolboxTriggers {
  @Prop() graph: Graph;
  @Prop({mutable: true}) triggerDescriptors: Array<TriggerDescriptor> = [];
  @Prop({mutable: true}) activityDescriptors: Array<ActivityDescriptor> = [];
  @State() triggerCategoryModels: Array<TriggerCategoryModel> = [];
  private dnd: Addon.Dnd;
  private renderedTriggers: Map<string, string>;

  @Watch('graph')
  handleGraphChanged(value: Graph) {

    if (this.dnd)
      this.dnd.dispose();

    this.dnd = new Addon.Dnd({
      target: value,
      scaled: false,
      animation: true,
    });
  }

  @Watch('triggerDescriptors')
  handleTriggerDescriptorsChanged(value: Array<TriggerDescriptor>) {
    const categorizedTriggersLookup = groupBy(value, x => x.category);
    const categories = Object.keys(categorizedTriggersLookup);
    const renderedTriggers: Map<string, string> = new Map<string, string>();

    // Group triggers by category
    this.triggerCategoryModels = categories.map(x => {
      const model: TriggerCategoryModel = {
        category: x,
        expanded: false,
        triggers: categorizedTriggersLookup[x]
      };

      return model;
    });

    // Render activities.
    const driverRegistry = Container.get(TriggerDriverRegistry);

    for (const triggerDescriptor of value) {
      const triggerType = triggerDescriptor.nodeType;
      const driver = driverRegistry.createDriver(triggerType);
      const html = driver.display({displayType: 'picker', triggerDescriptor: triggerDescriptor});

      renderedTriggers.set(triggerType, html);
    }

    this.renderedTriggers = renderedTriggers;
  }

  componentDidLoad() {
    this.handleTriggerDescriptorsChanged(this.triggerDescriptors);
  }

  private onTriggerStartDrag(e: DragEvent, triggerDescriptor: TriggerDescriptor) {
    const json = JSON.stringify(triggerDescriptor);
    const activityDescriptor = this.activityDescriptors.find(x => x.nodeType == triggerDescriptor.nodeType);
    const isActivity = !!activityDescriptor;

    e.dataTransfer.setData('trigger-descriptor', json);

    if (isActivity) {
      const activityJson = JSON.stringify(activityDescriptor);
      e.dataTransfer.setData('activity-descriptor', activityJson);
    }
  }

  private onToggleTriggerCategory(categoryModel: TriggerCategoryModel) {
    categoryModel.expanded = !categoryModel.expanded;
    this.triggerCategoryModels = [...this.triggerCategoryModels];
  }

  render() {
    const categoryModels = this.triggerCategoryModels;
    const renderedTriggers = this.renderedTriggers;

    return <nav class=" flex-1 px-2 space-y-1 font-sans text-sm text-gray-600">
      {categoryModels.map(categoryModel => {

          const category = categoryModel.category;
          const triggers = categoryModel.triggers;
          const categoryButtonClass = categoryModel.expanded ? 'rotate-90' : '';
          const categoryContentClass = categoryModel.expanded ? '' : 'hidden';

          return <div class="space-y-1">
            <button type="button"
                    onClick={() => this.onToggleTriggerCategory(categoryModel)}
                    class="text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium rounded-md focus:outline-none">
              <svg
                class={`${categoryButtonClass} text-gray-300 mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150`}
                viewBox="0 0 20 20" aria-hidden="true">
                <path d="M6 6L14 10L6 14V6Z" fill="currentColor"/>
              </svg>
              {category}
            </button>

            <div class={`space-y-1 ${categoryContentClass}`}>

              {triggers.map(trigger => {
                const triggerHtml = renderedTriggers.get(trigger.nodeType);

                return (
                  <div class="w-full flex items-center pl-10 pr-2 py-2">
                    <div class="cursor-move" onDragStart={e => this.onTriggerStartDrag(e, trigger)}>
                      <div innerHTML={triggerHtml} draggable={true}/>
                    </div>
                  </div>
                );
              })}

            </div>
          </div>;
        }
      )}

    </nav>
  }
}

WorkflowEditorTunnel.injectProps(ToolboxTriggers, ['activityDescriptors', 'triggerDescriptors']);
