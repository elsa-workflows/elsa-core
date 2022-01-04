import {Component, h, Prop, State, Watch} from "@stencil/core";
import {Addon, Graph} from '@antv/x6';
import groupBy from 'lodash/groupBy';
import {Container} from 'typedi';
import {ActivityDescriptor, TriggerDescriptor} from '../../../models';
import {ActivityDriverRegistry} from '../../../services';
import WorkflowEditorTunnel from '../state';

interface ActivityCategoryModel {
  category: string;
  activities: Array<ActivityDescriptor>;
  expanded: boolean;
}

@Component({
  tag: 'elsa-toolbox-activities',
})
export class ToolboxActivities {
  @Prop() graph: Graph;
  @Prop({mutable: true}) activityDescriptors: Array<ActivityDescriptor> = [];
  @Prop({mutable: true}) triggerDescriptors: Array<TriggerDescriptor> = [];
  @State() activityCategoryModels: Array<ActivityCategoryModel> = [];
  private dnd: Addon.Dnd;
  private renderedActivities: Map<string, string>;

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

  @Watch('activityDescriptors')
  handleActivityDescriptorsChanged(value: Array<ActivityDescriptor>) {
    const categorizedActivitiesLookup = groupBy(value, x => x.category);
    const categories = Object.keys(categorizedActivitiesLookup);
    const renderedActivities: Map<string, string> = new Map<string, string>();

    // Group activities by category
    this.activityCategoryModels = categories.map(x => {
      const model: ActivityCategoryModel = {
        category: x,
        expanded: false,
        activities: categorizedActivitiesLookup[x]
      };

      return model;
    });

    // Render activities.
    const activityDriverRegistry = Container.get(ActivityDriverRegistry);

    for (const activityDescriptor of value) {
      const nodeType = activityDescriptor.nodeType;
      const driver = activityDriverRegistry.createDriver(nodeType);
      const html = driver.display({displayType: 'picker', activityDescriptor: activityDescriptor});

      renderedActivities.set(nodeType, html);
    }

    this.renderedActivities = renderedActivities;
  }

  componentDidLoad() {
    this.handleActivityDescriptorsChanged(this.activityDescriptors);
  }

  private onActivityStartDrag(e: DragEvent, activityDescriptor: ActivityDescriptor) {
    const json = JSON.stringify(activityDescriptor);
    const triggerDescriptor = this.triggerDescriptors.find(x => x.nodeType == activityDescriptor.nodeType);
    const isTrigger = !!triggerDescriptor;

    e.dataTransfer.setData('activity-descriptor', json);

    if (isTrigger) {
      const triggerDescriptorJson = JSON.stringify(triggerDescriptor);
      e.dataTransfer.setData('trigger-descriptor', triggerDescriptorJson);
    }
  }

  private onToggleActivityCategory(categoryModel: ActivityCategoryModel) {
    categoryModel.expanded = !categoryModel.expanded;
    this.activityCategoryModels = [...this.activityCategoryModels];
  }

  render() {

    const categoryModels = this.activityCategoryModels;
    const renderedActivities = this.renderedActivities;

    return <nav class="flex-1 px-2 space-y-1 font-sans text-sm text-gray-600">
      {categoryModels.map(categoryModel => {

          const category = categoryModel.category;
          const activityDescriptors = categoryModel.activities;
          const categoryButtonClass = categoryModel.expanded ? 'rotate-90' : '';
          const categoryContentClass = categoryModel.expanded ? '' : 'hidden';

          return <div class="space-y-1">
            <button type="button"
                    onClick={() => this.onToggleActivityCategory(categoryModel)}
                    class="text-gray-600 hover:bg-gray-50 hover:text-gray-900 group w-full flex items-center pr-2 py-2 text-left text-sm font-medium rounded-md focus:outline-none">
              <svg
                class={`${categoryButtonClass} text-gray-300 mr-2 flex-shrink-0 h-5 w-5 transform group-hover:text-gray-400 transition-colors ease-in-out duration-150`}
                viewBox="0 0 20 20" aria-hidden="true">
                <path d="M6 6L14 10L6 14V6Z" fill="currentColor"/>
              </svg>
              {category}
            </button>

            <div class={`space-y-1 ${categoryContentClass}`}>

              {activityDescriptors.map(activityDescriptor => {
                const activityHtml = renderedActivities.get(activityDescriptor.nodeType);
                return (
                  <div class="w-full flex items-center pl-10 pr-2 py-2">
                    <div class="cursor-move"
                         onDragStart={e => this.onActivityStartDrag(e, activityDescriptor)}>
                      <div innerHTML={activityHtml} draggable={true}/>
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

WorkflowEditorTunnel.injectProps(ToolboxActivities, ['activityDescriptors', 'triggerDescriptors']);
